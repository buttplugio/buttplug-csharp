using System;
using Buttplug.Core;
using Buttplug.Messages;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using ButtplugKiirooPlatformEmulator;
#if (!WIN7)
using ButtplugUWPBluetoothManager.Core;
#endif
using ButtplugXInputGamepadManager.Core;
using NLog;
using NLog.Config;
using NLog.Targets;
using JetBrains.Annotations;
using Microsoft.Win32;
using SharpRaven;
using SharpRaven.Data;

namespace ButtplugGUI
{
    public class Device
    {
        public string Name { get; }
        public uint Index { get; }
        public string[] Messages { get;  }

        public Device(uint aIndex, string aName, string[] aMessages)
        {
            Index = aIndex;
            Name = aName;
            Messages = aMessages;
        }

        public override string ToString()
        {
            return $"{Index}: {Name}";
        }
    }

    public class DeviceList : ObservableCollection<Device>
    {
    }

    public class LogList : ObservableCollection<string>
    {
    }


    [Target("ButtplugGUILogger")]
    public sealed class ButtplugGUIMessageNLogTarget : TargetWithLayoutHeaderAndFooter
    {
        private readonly LogList _logs;
        private readonly Thread _winThread;

        public ButtplugGUIMessageNLogTarget(LogList l, Thread aWinThread)
        {
            // TODO This totally needs a mutex or something
            _logs = l;
            _winThread = aWinThread;
        }

        protected override void Write(LogEventInfo aLogEvent)
        {
            Dispatcher.FromThread(_winThread).Invoke(() => _logs.Add(this.Layout.Render(aLogEvent)));
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ButtplugService _bpServer;
        private readonly DeviceList _devices;
        private readonly LogList _logs;
        private readonly KiirooPlatformEmulator _kiirooEmulator;
        private readonly ButtplugGUIMessageNLogTarget _logTarget;
        private LoggingRule _outgoingLoggingRule;
        private readonly string _gitHash;
        private bool _sentCrashLog;
        private byte _clickCounter;

        public MainWindow()
        {
            _logs = new LogList();
            var c = LogManager.Configuration ?? new LoggingConfiguration();

            // Cover all of the possible bases for WPF failure
            // http://stackoverflow.com/questions/12024470/unhandled-exception-still-crashes-application-after-being-caught

            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            // Null check application, otherwise test bringup for GUI tests will fail
            if (Application.Current != null)
            {
                Application.Current.DispatcherUnhandledException += CurrentOnDispatcherUnhandledException;
            }
            // Null check Dispatcher, otherwise test bringup for GUI tests will fail.
            if (Dispatcher != null)
            {
                Dispatcher.UnhandledException += DispatcherOnUnhandledException;

                _logTarget = new ButtplugGUIMessageNLogTarget(_logs, Dispatcher.Thread);
                c.AddTarget("ButtplugGuiLogger", _logTarget);
                _outgoingLoggingRule = new LoggingRule("*", LogLevel.Debug, _logTarget);
                c.LoggingRules.Add(_outgoingLoggingRule);
                LogManager.Configuration = c;
            }

            // External Logger Setup
            //_msgTarget = new ButtplugMessageNLogTarget();
            //_msgTarget.LogMessageReceived += LogMessageReceivedHandler;
            //c.AddTarget("ButtplugLogger", _msgTarget);
            //_outgoingLoggingRule = new LoggingRule("*", LogLevel.Off, _msgTarget);
            //c.LoggingRules.Add(_outgoingLoggingRule);

#if DEBUG
            // Debug Logger Setup
            var t = new DebuggerTarget();
            LogManager.Configuration.AddTarget("debugger", t);
            LogManager.Configuration.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, t));
            LogManager.Configuration = LogManager.Configuration;
#endif

            // Set up internal services
            _bpServer = new ButtplugService();
            _bpServer.MessageReceived += OnMessageReceived;
#if (!WIN7)
            _bpServer.AddDeviceSubtypeManager(aLogger => new UWPBluetoothManager(aLogger));
#endif
            _bpServer.AddDeviceSubtypeManager(aLogger => new XInputGamepadManager(aLogger));
            _devices = new DeviceList();

            _kiirooEmulator = new KiirooPlatformEmulator();
            _kiirooEmulator.OnKiirooPlatformEvent += HandleKiirooPlatformMessage;

            // Set up GUI
            InitializeComponent();
            LogLevelComboBox.SelectionChanged += LogLevelSelectionChangedHandler;
            DeviceListBox.ItemsSource = _devices;
            KiirooListBox.ItemsSource = _devices;
            LogListBox.ItemsSource = _logs;
            try
            {
                AboutVersionNumber.Text = Assembly.GetEntryAssembly().GetName().Version.ToString();
                _gitHash = System.Diagnostics.FileVersionInfo.GetVersionInfo(Application.ResourceAssembly.Location)
                    .ProductVersion;
                if (_gitHash.Length > 0)
                {
                    AboutVersionNumber.Text += $"-{_gitHash.Substring(0, 8)}";
                    AboutVersionNumber.MouseDown += GithubRequestNavigate;
                }
                Title = $"Buttplug {AboutVersionNumber.Text}";
            }
            catch (Exception)
            {
                // TODO Make this catch far more granular
                var log = LogManager.GetCurrentClassLogger();
                log.Info("Can't load assembly file, no version info available!");
            }
        }

        private void SendExceptionToSentry(Exception aEx)
        {
            if (_sentCrashLog)
            {
                return;
            }
            _sentCrashLog = true;
            AppDomain.CurrentDomain.UnhandledException -= CurrentDomainOnUnhandledException;
            if (Application.Current != null)
            {
                Application.Current.DispatcherUnhandledException -= CurrentOnDispatcherUnhandledException;
            }
            if (Dispatcher != null)
            {
                Dispatcher.UnhandledException -= DispatcherOnUnhandledException;
            }

            var _ravenClient = new RavenClient("https://2e376d00cdcb44bfb2140c1cf000d73b:1fa6980aeefa4b048b866a450ee9ad71@sentry.io/170313");
            _ravenClient.Capture(new SentryEvent(aEx));
        }

        private void DispatcherOnUnhandledException(object aObj, DispatcherUnhandledExceptionEventArgs aEx)
        {
            SendExceptionToSentry(aEx.Exception);
        }

        private void CurrentDomainOnUnhandledException(object aObj, UnhandledExceptionEventArgs aEx)
        {
            SendExceptionToSentry(aEx.ExceptionObject as Exception);
        }

        private void CurrentOnDispatcherUnhandledException(object aObj, DispatcherUnhandledExceptionEventArgs aEx)
        {
            SendExceptionToSentry(aEx.Exception);
        }

        private void OnMessageReceived(object o, MessageReceivedEventArgs e)
        {
            Dispatcher.InvokeAsync(() =>
            {
                switch (e.Message)
                {
                    case DeviceAdded m:
                        _devices.Add(new Device(m.DeviceIndex, m.DeviceName, m.DeviceMessages));
                        break;
                    case DeviceRemoved d:
                        var device = from dl in _devices
                            where dl.Index == d.DeviceIndex
                            select dl;
                        foreach (var dr in device.ToList())
                        {
                            _devices.Remove(dr);
                        }
                        break;
                }
            });
        }

        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            // Disable button until we're done here
            ScanButton.Click -= ScanButton_Click;
            if ((string)ScanButton.Content == "Start Scanning")
            {
                await _bpServer.SendMessage(new StartScanning());
                ScanButton.Content = "Stop Scanning";
            }
            else
            {
                await _bpServer.SendMessage(new StopScanning());
                ScanButton.Content = "Start Scanning";
            }
            ScanButton.Click += ScanButton_Click;
        }

        public async void HandleKiirooPlatformMessage(object o, KiirooPlatformEventArgs e)
        {
            await Dispatcher.InvokeAsync(async () =>
            {
                var currentDevices = KiirooListBox.SelectedItems.Cast<Device>().ToList();
                foreach (var device in currentDevices)
                {
                    if (!device.Messages.Contains("KiirooRawCmd"))
                    {
                        continue;
                    }
                    await _bpServer.SendMessage(new KiirooRawCmd(device.Index, e.Position));
                }
            });
        }

        private void ApplicationSelector_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            WebsocketSettingsGrid.Visibility = Visibility.Hidden;
            KiirooSettingsGrid.Visibility = Visibility.Hidden;
            if (ApplicationSelector.SelectedItem == ApplicationNone)
            {
                _kiirooEmulator.StopServer();
            }
            else if (ApplicationSelector.SelectedItem == ApplicationWebsockets)
            {
                WebsocketSettingsGrid.Visibility = Visibility.Visible;
                _kiirooEmulator.StopServer();
            }
            else if (ApplicationSelector.SelectedItem == ApplicationKiiroo)
            {
                KiirooSettingsGrid.Visibility = Visibility.Visible;
                _kiirooEmulator.StartServer();
            }
        }

        private void SaveLogFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                CheckFileExists = false,
                CheckPathExists = true,
                OverwritePrompt = true
            };
            if (dialog.ShowDialog() != true)
            {
                return;
            }
            var sw = new System.IO.StreamWriter(dialog.FileName, false);
            foreach (var line in _logs.ToList())
            {
                sw.WriteLine(line);
            }
            sw.Close();
        }

        private void LogLevelSelectionChangedHandler(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {            
            var c = LogManager.Configuration;
            var level = ((ComboBoxItem)LogLevelComboBox.SelectedValue).Content.ToString();
            try
            {
                c.LoggingRules.Remove(_outgoingLoggingRule);
                _outgoingLoggingRule = new LoggingRule("*", LogLevel.FromString(level), _logTarget);
                c.LoggingRules.Add(_outgoingLoggingRule);
                LogManager.Configuration = c;                
            }
            catch (ArgumentException)
            {
                LogManager.GetCurrentClassLogger().Error($"Log Level \"{level}\" is not a valid log level!");
            }
        }

        private void GithubRequestNavigate(object o, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start(
                new Uri($"http://github.com/metafetish/buttplug-csharp/commit/{_gitHash}").AbsoluteUri);
        }

        private void PatreonRequestNavigate(object o, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start(new Uri("http://patreon.com/qdot").AbsoluteUri);
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
        }

        private void CrashButton_Click(object sender, RoutedEventArgs e)
        {
            throw new Exception("Should be caught and sent to sentry!");
        }

        private void IconImage_Click(object sender, RoutedEventArgs e)
        {
            _clickCounter += 1;
            if (_clickCounter < 5)
            {
                return;
            }
            DeveloperTab.Visibility = Visibility.Visible;
            IconImage.MouseDown -= IconImage_Click;
        }
    }
}
