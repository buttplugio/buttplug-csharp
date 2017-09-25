using Buttplug.Client;
using Buttplug.Components.Controls;
using Buttplug.Core.Messages;
using System;
using System.Collections.Concurrent;
using System.Windows;
using System.Windows.Threading;

namespace Buttplug.Apps.ExampleClientGUI
{
    public partial class ExampleClientPanel
    {
        public ConcurrentDictionary<uint, ButtplugClientDevice> Devices = new ConcurrentDictionary<uint, ButtplugClientDevice>();

        private ButtplugWSClient _client;

        public ExampleClientPanel()
        {
            InitializeComponent();
            _client = new ButtplugWSClient("Example Client");
        }

        private void ConnToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (_client.IsConnected)
            {
                _client.Disconnect().Wait();
                ConnToggleButton.Content = "Connect";
                AdressTextBox.IsEnabled = true;
            }
            else
            {
                ConnToggleButton.Content = "Disconnect";
                AdressTextBox.IsEnabled = false;
                _client.Connect(new Uri(AdressTextBox.Text));
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