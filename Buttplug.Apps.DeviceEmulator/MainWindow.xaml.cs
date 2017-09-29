using Buttplug.DeviceSimulator.PipeMessages;
using System;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Buttplug.Apps.DeviceEmulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private NamedPipeClientStream _pipeClient;

        private Task _readThread;

        private CancellationTokenSource _tokenSource;

        private PipeMessageParser _parser;

        public MainWindow()
        {
            InitializeComponent();
            _parser = new PipeMessageParser();
        }

        private void Connect_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                _pipeClient = new NamedPipeClientStream(".",
                            "ButtplugDeviceSimulator",
                            PipeDirection.InOut, PipeOptions.Asynchronous,
                            TokenImpersonationLevel.Impersonation);
                _pipeClient.Connect(2000);
                Connect.Content = "Disconnect";

                _tokenSource = new CancellationTokenSource();
                _readThread = new Task(() => { pipeReader(_tokenSource.Token); }, _tokenSource.Token, TaskCreationOptions.LongRunning);
                _readThread.Start();
            }
            catch
            {
                if (_pipeClient.IsConnected)
                {
                    _pipeClient.Close();
                }

                _pipeClient = null;
                Connect.Content = "Connect";
            }
        }

        private async void pipeReader(CancellationToken aCancellationToken)
        {
            while (!aCancellationToken.IsCancellationRequested && _pipeClient != null && _pipeClient.IsConnected)
            {

                var buffer = new byte[4096];
                string msg = string.Empty;
                var len = -1;
                while (len < 0 || (len == buffer.Length && buffer[4095] != '\0'))
                {
                    var waiter = _pipeClient.ReadAsync(buffer, 0, buffer.Length);
                    while (!waiter.GetAwaiter().IsCompleted)
                    {
                        if (!_pipeClient.IsConnected)
                        {
                            return;
                        }

                        Thread.Sleep(100);
                    }

                    len = waiter.GetAwaiter().GetResult();

                    if (len > 0)
                    {
                        msg += Encoding.ASCII.GetString(buffer, 0, len);
                    }
                }

                var parsed = _parser.Deserialize(msg);
                if (parsed == null)
                {
                    continue;
                }

                switch (parsed)
                {
                    case StartScanning s1:
                        foreach (TabItem tab in DeviceTabs.Items)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                var dev = (tab.Content as DeviceSimulator);
                                if (dev == null)
                                {
                                    return;
                                }

                                var devAdded = new DeviceAdded();
                                devAdded.Name = dev.DeviceName.Text;
                                devAdded.Id = dev.DeviceId.Text;

                                byte[] bytes = Encoding.ASCII.GetBytes(_parser.Serialize(devAdded));
                                _pipeClient.Write(bytes, 0, bytes.Length);
                            });
                        }

                        byte[] bytes2 = Encoding.ASCII.GetBytes(_parser.Serialize(new FinishedScanning()));
                        _pipeClient.Write(bytes2, 0, bytes2.Length);
                        break;

                    case StopScanning s1:
                        break;

                    case StopDevice sd:
                        foreach (TabItem tab in DeviceTabs.Items)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                var dev = (tab.Content as DeviceSimulator);
                                if (dev == null || dev.DeviceId.Text != sd.Id)
                                {
                                    return;
                                }

                                if(dev.DeviceHasVibrator.IsChecked.GetValueOrDefault(false))
                                {
                                    dev.VibratorSpeed.Value = 0;
                                }
                            });
                        }
                        break;

                    case Vibrate v:
                        foreach (TabItem tab in DeviceTabs.Items)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                var dev = (tab.Content as DeviceSimulator);
                                if (dev == null || dev.DeviceId.Text != v.Id)
                                {
                                    return;
                                }

                                if (dev.DeviceHasVibrator.IsChecked.GetValueOrDefault(false))
                                {
                                    dev.VibratorSpeed.Value = Math.Min(v.Speed, 1) * dev.VibratorSpeed.Maximum;
                                }
                            });
                        }
                        break;

                    default:
                        break;
                }
            }

            if (_pipeClient != null && _pipeClient.IsConnected)
            {
                _pipeClient.Close();
            }

            _pipeClient = null;
            Connect.Content = "Connect";
        }

        private void AddDevice_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var tab = new TabItem();
            tab.Content = new DeviceSimulator(DeviceTabs.Items.Count);
            tab.Header = (tab.Content as DeviceSimulator).DeviceName.Text;
            DeviceTabs.Items.Add(tab);
            DeviceTabs.SelectedIndex = DeviceTabs.Items.Count - 1;
        }
        
    }
}
