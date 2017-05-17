using System;
using System.Collections.Generic;
using Buttplug.Core;
using Buttplug.Messages;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ButtplugKiirooPlatformEmulator;
using Microsoft.Win32;
using NLog;
using NLog.Config;
using NLog.Targets;

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
        private readonly ButtplugGUIMessageNLogTarget _logTarget;
        private KiirooPlatformEmulator _kiirooEmulator;
        private LoggingRule _outgoingLoggingRule;
        private string _gitHash;

        public MainWindow()
        {
            _logs = new LogList();
            _logTarget = new ButtplugGUIMessageNLogTarget(_logs, Dispatcher.Thread);
            // External Logger Setup
            var c = LogManager.Configuration ?? new LoggingConfiguration();
            c.AddTarget("ButtplugGuiLogger", _logTarget);
            _outgoingLoggingRule = new LoggingRule("*", LogLevel.Debug, _logTarget);
            c.LoggingRules.Add(_outgoingLoggingRule);
            LogManager.Configuration = c;

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
            _bpServer.AddDeviceSubtypeManager((x) => new BluetoothManager(x));
#endif
            _bpServer.AddDeviceSubtypeManager((x) => new XInputGamepadManager(x));
            _devices = new DeviceList();

            _kiirooEmulator = new KiirooPlatformEmulator();
            _kiirooEmulator.OnKiirooPlatformEvent += HandleKiirooPlatformMessage;
            
            
            // Set up GUI
            InitializeComponent();
            LogLevelComboBox.SelectionChanged += LogLevelSelectionChangedHandler;
            DeviceListBox.ItemsSource = _devices;
            KiirooListBox.ItemsSource = _devices;
            LogListBox.ItemsSource = _logs;
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

        public void OnMessageReceived(object o, MessageReceivedEventArgs e)
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
    }
}