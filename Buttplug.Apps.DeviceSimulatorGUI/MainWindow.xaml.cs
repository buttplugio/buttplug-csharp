using Buttplug.DeviceSimulator.PipeMessages;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace Buttplug.Apps.DeviceSimulatorGUI
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

        private Dictionary<int, DeviceSimulator> tabs = new Dictionary<int, DeviceSimulator>();

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

                        Thread.Sleep(10);
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
                        foreach (DeviceSimulator dev in tabs.Values)
                        {
                            var devAdded = new DeviceAdded();
                            devAdded.Name = dev.Name;
                            devAdded.Id = dev.Id;
                            devAdded.HasLinear = dev.HasLinear;
                            devAdded.HasVibrator = dev.HasVibrator;
                            devAdded.HasRotator = dev.HasRotator;

                            byte[] bytes = Encoding.ASCII.GetBytes(_parser.Serialize(devAdded));
                            _pipeClient.Write(bytes, 0, bytes.Length);
                        }

                        byte[] bytes2 = Encoding.ASCII.GetBytes(_parser.Serialize(new FinishedScanning()));
                        _pipeClient.Write(bytes2, 0, bytes2.Length);
                        break;

                    case StopScanning s1:
                        break;

                    case StopDevice sd:
                        foreach (DeviceSimulator dev in tabs.Values)
                        {
                            if (dev == null || dev.Id != sd.Id)
                            {
                                return;
                            }

                            Dispatcher.Invoke(() =>
                            {
                                if (dev.DeviceHasVibrator.IsChecked.GetValueOrDefault(false))
                                {
                                    dev.VibratorSpeed.Value = 0;
                                }
                                if (dev.DeviceHasRotator.IsChecked.GetValueOrDefault(false))
                                {
                                    dev.RotatorSpeed.Value = 0;
                                }
                                if (dev.DeviceHasLinear.IsChecked.GetValueOrDefault(false))
                                {
                                    // Complicated stuff: position stays the same
                                }
                            });
                        }
                        break;

                    case Vibrate v:
                        foreach (DeviceSimulator dev in tabs.Values)
                        {
                            if (dev == null || dev.Id != v.Id)
                            {
                                return;
                            }

                            if (dev.HasVibrator)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    dev.VibratorSpeed.Value = Math.Min(v.Speed, 1) * dev.VibratorSpeed.Maximum;
                                });
                            }
                        }
                        break;

                    case Linear l:
                        foreach (DeviceSimulator dev in tabs.Values)
                        {
                            if (dev == null || dev.Id != l.Id)
                            {
                                return;
                            }

                            if (dev.HasLinear)
                            {
                                dev.LinearTargetPosition = Math.Min(l.Position, 99);
                                dev.LinearSpeed = Math.Min(l.Speed, 99);
                            }
                        }
                        break;

                    case Rotate r:
                        foreach (DeviceSimulator dev in tabs.Values)
                        {
                            if (dev == null || dev.Id != r.Id)
                            {
                                return;
                            }

                            if (dev.HasRotator)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    dev.RotatorSpeed.Value = (Convert.ToDouble(Math.Min(r.Speed, 99)) / 100) * dev.RotatorSpeed.Maximum;
                                    dev.RotatorSpeed.Foreground = r.Clockwise ? Brushes.Red : Brushes.GreenYellow;
                                });
                            }
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
            var sim = new DeviceSimulator(DeviceTabs.Items.Count);
            tab.Content = sim;
            tab.Header = (tab.Content as DeviceSimulator).DeviceName.Text;
            tabs.Add(DeviceTabs.Items.Count, sim);
            DeviceTabs.Items.Add(tab);
            DeviceTabs.SelectedIndex = DeviceTabs.Items.Count - 1;
        }
        
    }
}
