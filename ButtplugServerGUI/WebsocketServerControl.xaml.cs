using System;
using System.Windows;
using System.Windows.Controls;
using ButtplugWebsockets;
using Buttplug.Core;

namespace ButtplugServerGUI
{
    /// <summary>
    /// Interaction logic for WebsocketServerControl.xaml
    /// </summary>
    public partial class WebsocketServerControl : UserControl
    {
        private ButtplugWebsocketServer _ws;
        private ButtplugService _service;
        private uint _port;
        private bool _secure;
        private ButtplugConfig _config;

        public WebsocketServerControl(ButtplugService aService)
        {
            InitializeComponent();
            _ws = new ButtplugWebsocketServer();
            _config = new ButtplugConfig("Buttplug");
            _service = aService;
            _port = 12345;
            _secure = false;
            if (UInt32.TryParse(_config.GetValue("buttplug.server.port", "12345"), out uint pres))
            {
                _port = pres;
            }
            if (Boolean.TryParse(_config.GetValue("buttplug.server.secure", "false"), out bool sres))
            {
                _secure = sres;
            }
            ((TextBox)PortTextBox).Text = _port.ToString();
            ((CheckBox)SecureCheckBox).IsChecked = _secure;
        }

        public void StartServer()
        {
            _ws.StartServer(_service, (int) _port, _secure);
            ((Button)ConnToggleButton).Content = "Stop";
            ((CheckBox)SecureCheckBox).IsEnabled = false;
            ((TextBox)PortTextBox).IsEnabled = false;
        }
        public void StopServer()
        {
            _ws.StopServer();
            ((Button)ConnToggleButton).Content = "Start";
            ((CheckBox)SecureCheckBox).IsEnabled = true;
            ((TextBox)PortTextBox).IsEnabled = true;
        }

        private void ConnToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)ConnToggleButton).Content.Equals("Start"))
            {
                StartServer();
                _config.SetValue("buttplug.server.port", _port.ToString());
                _config.SetValue("buttplug.server.secure", _secure.ToString());
            }
            else
            {
                StopServer();
            }
        }
       

        private void PortTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (UInt32.TryParse(((TextBox)PortTextBox).Text, out uint port) && port >= 1024 && port <= 65535)
            {
                _port = port;
                return;
            }
            ((TextBox)PortTextBox).Text = _port.ToString();
        }

        private void SecureCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _secure = false;
        }

        private void SecureCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _secure = true;
        }
    }
}
