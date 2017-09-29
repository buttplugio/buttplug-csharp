using Buttplug.Client;
using Buttplug.Components.Controls;
using Buttplug.Core.Messages;
using System;
using System.Collections.Concurrent;
using System.Windows;
using System.Windows.Threading;
using static Buttplug.Client.DeviceEventArgs;

namespace Buttplug.Apps.ExampleClientGUI
{
    public partial class ExampleClientPanel
    {
        public ConcurrentDictionary<uint, ButtplugClientDevice> Devices = new ConcurrentDictionary<uint, ButtplugClientDevice>();

        private ButtplugWSClient _client;

        private ButtplugDeviceControl devControl;

        public ExampleClientPanel(ButtplugDeviceControl aDevControl)
        {
            InitializeComponent();
            devControl = aDevControl;

            devControl.StartScanning += OnStartScanning;
            devControl.StopScanning += OnStopScanning;
        }

        private async void ConnToggleButton_Click(object sender, RoutedEventArgs e)
        {
            ConnToggleButton.IsEnabled = false;
            if (ConnToggleButton.Content == "Disconnect")
            {
                if (_client != null)
                {
                    await _client.Disconnect();
                    _client = null;
                    devControl.Reset();
                }

                ConnToggleButton.Content = "Connect";
                AdressTextBox.IsEnabled = true;
            }
            else
            {
                ConnToggleButton.Content = "Disconnect";
                AdressTextBox.IsEnabled = false;
                if (_client == null)
                {
                    devControl.Reset();
                    _client = new ButtplugWSClient("Example Client");

                    _client.DeviceAdded += OnDeviceChanged;
                    _client.DeviceRemoved += OnDeviceChanged;

                    Connect();
                }
            }

            ConnToggleButton.IsEnabled = true;
        }

        private async void Connect()
        {
            if (_client != null)
            {
                await _client.Connect(new Uri(AdressTextBox.Text));
                await _client.RequestDeviceList();

                foreach (var dev in _client.getDevices())
                {
                    devControl.DeviceAdded(new ButtplugDeviceInfo(dev.Index, dev.Name, dev.AllowedMessages.ToArray()));
                }
            }
        }

        private void OnDeviceChanged(object sender, DeviceEventArgs e)
        {
            switch (e.Action)
            {
                case DeviceAction.ADDED:
                    devControl.DeviceAdded(new ButtplugDeviceInfo(e.Device.Index, e.Device.Name, e.Device.AllowedMessages.ToArray()));
                    break;

                case DeviceAction.REMOVED:
                    devControl.DeviceRemoved(e.Device.Index);
                    break;
            }
        }

        private void OnStartScanning(object sender, EventArgs args)
        {
            if (_client != null)
            {
                _client.StartScanning();
            }
        }

        private void OnStopScanning(object sender, EventArgs args)
        {
            if (_client != null)
            {
                _client.StopScanning();
            }
        }

        private void SendLinear_Click(object sender, RoutedEventArgs e)
        {
            if (!_client.IsConnected)
            {
                return;
            }

            foreach (var dev in Devices.Values)
            {
                if (dev.AllowedMessages.Contains("FleshlightLaunchFW12Cmd"))
                {
                    _client.SendDeviceMessage(dev,
                        new FleshlightLaunchFW12Cmd(dev.Index,
                            Convert.ToUInt32(LinearSpeed.Value),
                            Convert.ToUInt32(LinearPosition.Value),
                            _client.nextMsgId));
                }
            }
        }

        private void SendVibrate_Click(object sender, RoutedEventArgs e)
        {
            if (!_client.IsConnected)
            {
                return;
            }

            foreach (var dev in Devices.Values)
            {
                if (dev.AllowedMessages.Contains("SingleMotorVibrateCmd"))
                {
                    _client.SendDeviceMessage(dev,
                        new SingleMotorVibrateCmd(dev.Index,
                            Convert.ToDouble(VibrateSpeed.Value),
                            _client.nextMsgId));
                }
            }
        }

        private void SendRotate_Click(object sender, RoutedEventArgs e)
        {
            if (!_client.IsConnected)
            {
                return;
            }

            foreach (var dev in Devices.Values)
            {
                if (dev.AllowedMessages.Contains("VorzeA10CycloneCmd"))
                {
                    bool clockwise = RotateSpeed.Value > 0;
                    _client.SendDeviceMessage(dev,
                        new VorzeA10CycloneCmd(dev.Index,
                            Convert.ToUInt32(RotateSpeed.Value * (clockwise ? 1 : -1)),
                            clockwise,
                            _client.nextMsgId));
                }
            }
        }
    }
}