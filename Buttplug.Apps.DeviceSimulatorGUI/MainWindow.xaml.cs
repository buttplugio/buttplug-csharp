using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using Buttplug.DeviceSimulator.PipeMessages;
using Buttplug.Server.Util;

namespace Buttplug.Apps.DeviceSimulatorGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private NamedPipeClientStream _pipeClient;

        private Task _readThread;
        private Task _writeThread;

        private CancellationTokenSource _tokenSource;

        private PipeMessageParser _parser;

        private Dictionary<int, DeviceSimulator> tabs = new Dictionary<int, DeviceSimulator>();

        private ConcurrentQueue<IDeviceSimulatorPipeMessage> _msgQueue = new ConcurrentQueue<IDeviceSimulatorPipeMessage>();

        public MainWindow()
        {
            InitializeComponent();
            _parser = new PipeMessageParser();
        }

        private void Connect_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (Connect.Content as string == "Disconnect")
            {
                if (_pipeClient.IsConnected)
                {
                    _pipeClient.Close();
                }

                _pipeClient = null;
                Connect.Content = "Connect";
                return;
            }

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

                _writeThread = new Task(() => { pipeWriter(_tokenSource.Token); }, _tokenSource.Token, TaskCreationOptions.LongRunning);
                _writeThread.Start();
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

        private void pipeReader(CancellationToken aCancellationToken)
        {
            while (!aCancellationToken.IsCancellationRequested && _pipeClient != null && _pipeClient.IsConnected)
            {
                var buffer = new byte[4096];
                string msg = string.Empty;
                var len = -1;
                while (len < 0 || (len == buffer.Length && buffer[4095] != '\0'))
                {
                    try
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
                    }
                    catch
                    {
                        continue;
                    }

                    if (len > 0)
                    {
                        msg += Encoding.ASCII.GetString(buffer, 0, len);
                    }
                }

                Console.Out.WriteLine(msg);
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
                            devAdded.VibratorCount = dev.VibratorCount;
                            devAdded.HasRotator = dev.HasRotator;

                            _msgQueue.Enqueue(devAdded);
                        }

                        _msgQueue.Enqueue(new FinishedScanning());
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

                            dev.StopDevice();
                        }

                        break;

                    case Vibrate v:
                        foreach (DeviceSimulator dev in tabs.Values)
                        {
                            if (dev == null || dev.Id != v.Id)
                            {
                                return;
                            }

                            dev.Vibrate(v.VibratorId, v.Speed);
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
                                dev.LinearTargetPosition = l.Position;
                                dev.LinearSpeed = l.Speed;
                            }
                        }

                        break;

                    case Linear2 l:
                        foreach (DeviceSimulator dev in tabs.Values)
                        {
                            if (dev == null || dev.Id != l.Id)
                            {
                                return;
                            }

                            if (dev.HasLinear)
                            {
                                double dist = Math.Abs(dev.LinearTargetPosition - l.Position);
                                dev.LinearSpeed = FleshlightHelper.GetSpeed(dist, l.Duration);
                                dev.LinearTargetPosition = l.Position;
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

                    case Ping p:
                        _msgQueue.Enqueue(new Ping());
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
            Dispatcher.Invoke(() =>
            {
                Connect.Content = "Connect";
            });
        }

        private void pipeWriter(CancellationToken aCancellationToken)
        {
            while (!aCancellationToken.IsCancellationRequested)
            {
                if (_pipeClient != null && _pipeClient.IsConnected && _msgQueue.TryDequeue(out IDeviceSimulatorPipeMessage msg))
                {
                    var str = _parser.Serialize(msg);
                    if (str != null)
                    {
                        try
                        {
                            _pipeClient.Write(Encoding.ASCII.GetBytes(str), 0, str.Length);
                        }
                        catch
                        {
                        }
                    }
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
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
