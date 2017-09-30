using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Buttplug.Apps.DeviceSimulatorGUI
{
    /// <summary>
    /// Interaction logic for DeviceSimulation.xaml
    /// </summary>
    public partial class DeviceSimulator
    {
        private int tabId;

        public string Id;

        public new string Name;

        public bool HasLinear;

        public bool HasVibrator;

        public bool HasRotator;

        internal uint LinearTargetPosition;

        internal double LinearCurrentPosition;

        internal long LinearCurrentTime;

        internal uint LinearSpeed;


        private Task _posThread;

        private CancellationTokenSource _tokenSource;
        private double _linearPosMax;

        public DeviceSimulator(int aTabId)
        {
            InitializeComponent();
            tabId = aTabId;
            DeviceName.Text = "Device" + tabId;
            Name = Id = DeviceId.Text = DeviceName.Text;
            DeviceId.TextChanged += IdChangedEventHandler;
            DeviceName.TextChanged += NameChangedEventHandler;
            HasRotator = HasVibrator = HasLinear = false;
            DeviceHasLinear.Checked += LinearCheckedEventHandler;
            DeviceHasVibrator.Checked += VibratorCheckedEventHandler;
            DeviceHasRotator.Checked += RotatorCheckedEventHandler;

            _linearPosMax = LinearPosition.Maximum;

            _tokenSource = new CancellationTokenSource();
            _posThread = new Task(() => { posUpdater(_tokenSource.Token); }, _tokenSource.Token, TaskCreationOptions.LongRunning);
            _posThread.Start();
        }


        private async void posUpdater(CancellationToken aCancellationToken)
        {
            LinearCurrentTime = DateTime.Now.Ticks;
            while (!aCancellationToken.IsCancellationRequested)
            {
                var now = DateTime.Now.Ticks;
                if (LinearCurrentPosition != LinearTargetPosition)
                {
                    var diff = distance(now - LinearCurrentTime, LinearSpeed) * _linearPosMax;
                    var diff2 = LinearTargetPosition - LinearCurrentPosition;
                    if (diff2 < 0)
                    {
                        diff *= -1;
                        diff = Math.Max(diff2, diff);
                    }
                    else
                    {
                        diff = Math.Min(diff2, diff);
                    }
                    LinearCurrentPosition += diff;

                    Dispatcher.Invoke(() =>
                    {
                        LinearPosition.Value = LinearCurrentPosition;
                    });
                }
                LinearCurrentTime = now;
                Thread.Sleep(10);
            }
        }

        private double distance(long time, double speed)
        {
            var mil = Math.Pow(speed / 25000, -0.95);
            var diff = mil - (Convert.ToDouble(time) / 1e6);
            return 90 - ((diff / mil) * 90);
        }


        private void IdChangedEventHandler(object o, TextChangedEventArgs args)
        {
            Id = DeviceId.Text;
        }

        private void NameChangedEventHandler(object o, TextChangedEventArgs args)
        {
            Name = DeviceName.Text;
        }

        private void LinearCheckedEventHandler(object o, RoutedEventArgs args)
        {
            HasLinear = DeviceHasLinear.IsChecked.GetValueOrDefault(false);
        }

        private void VibratorCheckedEventHandler(object o, RoutedEventArgs args)
        {
            HasVibrator = DeviceHasVibrator.IsChecked.GetValueOrDefault(false);
        }

        private void RotatorCheckedEventHandler(object o, RoutedEventArgs args)
        {
            HasRotator = DeviceHasRotator.IsChecked.GetValueOrDefault(false);
        }
    }
}
