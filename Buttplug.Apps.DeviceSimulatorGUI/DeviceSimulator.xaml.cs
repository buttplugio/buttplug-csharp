using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Buttplug.Server.Util;

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

        public uint VibratorCount = 0;

        public bool HasRotator;

        internal double LinearTargetPosition;

        internal double LinearCurrentPosition;

        internal long LinearCurrentTime;

        internal double LinearSpeed;

        private Task _posThread;

        private CancellationTokenSource _tokenSource;

        private Dictionary<uint, VibratorRow> _vibrators = new Dictionary<uint, VibratorRow>();

        public DeviceSimulator(int aTabId)
        {
            InitializeComponent();
            tabId = aTabId;
            DeviceName.Text = "Device" + tabId;
            Name = Id = DeviceId.Text = DeviceName.Text;
            DeviceId.TextChanged += IdChangedEventHandler;
            DeviceName.TextChanged += NameChangedEventHandler;
            HasRotator = HasLinear = false;
            DeviceHasLinear.Checked += LinearCheckedEventHandler;
            DeviceHasRotator.Checked += RotatorCheckedEventHandler;

            LinearCurrentPosition = 0;

            _tokenSource = new CancellationTokenSource();
            _posThread = new Task(() => { posUpdater(_tokenSource.Token); }, _tokenSource.Token, TaskCreationOptions.LongRunning);
            _posThread.Start();
        }

        private void posUpdater(CancellationToken aCancellationToken)
        {
            LinearCurrentTime = DateTime.Now.Ticks;
            while (!aCancellationToken.IsCancellationRequested)
            {
                var now = DateTime.Now.Ticks;
                if (LinearCurrentPosition != LinearTargetPosition)
                {
                    var diff = FleshlightHelper.GetDistance(Convert.ToUInt32(new TimeSpan(now - LinearCurrentTime).TotalMilliseconds), LinearSpeed);
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
                        LinearPosition.Value = LinearCurrentPosition * LinearPosition.Maximum;
                    });
                }

                LinearCurrentTime = now;
                Thread.Sleep(10);
            }
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

        private void RotatorCheckedEventHandler(object o, RoutedEventArgs args)
        {
            HasRotator = DeviceHasRotator.IsChecked.GetValueOrDefault(false);
        }

        private class VibratorRow
        {
            public Label RowLabel;
            public ProgressBar RowProgress;

            public VibratorRow(int vId)
            {
                RowLabel = new Label();
                RowLabel.Name = "VibratorLabel" + vId;
                RowLabel.Content = "Vibrator" + vId + " Speed:";
                RowProgress = new ProgressBar();
                RowProgress.Name = "VibratorSpeed" + vId;
                RowProgress.Maximum = 1;

                Grid.SetRow(RowLabel, vId);
                Grid.SetColumn(RowLabel, 0);
                Grid.SetRow(RowProgress, vId);
                Grid.SetColumn(RowProgress, 1);
            }
        }

        private void DeviceHasVibrator_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            // Lock
            DeviceHasVibrator.IsEnabled = false;

            while (e.NewValue > VibratorCount)
            {
                // Add a vibe row
                var vId = VibratorCount++;
                var rowDef = new RowDefinition();
                rowDef.Height = GridLength.Auto;
                Vibrators.RowDefinitions.Add(rowDef);

                var row = new VibratorRow((int)vId);
                Vibrators.Children.Add(row.RowLabel);
                Vibrators.Children.Add(row.RowProgress);
                _vibrators.Add(vId, row);
            }

            while (e.NewValue < VibratorCount)
            {
                // Remove a vibe row
                var vId = --VibratorCount;

                if (_vibrators.TryGetValue(vId, out var row))
                {
                    Vibrators.Children.Remove(row.RowLabel);
                    Vibrators.Children.Remove(row.RowProgress);
                }

                Vibrators.RowDefinitions.RemoveAt((int)vId);
                _vibrators.Remove(vId);
            }

            // Unlock
            DeviceHasVibrator.IsEnabled = true;
        }

        public void StopDevice()
        {
            Dispatcher.Invoke(() =>
            {
                foreach (var j in _vibrators)
                {
                    j.Value.RowProgress.Value = 0;
                }

                if (DeviceHasRotator.IsChecked.GetValueOrDefault(false))
                {
                    RotatorSpeed.Value = 0;
                }

                if (DeviceHasLinear.IsChecked.GetValueOrDefault(false))
                {
                    // Complicated stuff: position stays the same
                }
            });
        }

        public void Vibrate(uint aId, double aSpeed)
        {
            Dispatcher.Invoke(() =>
            {
                if (_vibrators.TryGetValue(aId, out var row))
                {
                    row.RowProgress.Value = Math.Min(aSpeed, 1) * row.RowProgress.Maximum;
                }
            });
        }
    }
}
