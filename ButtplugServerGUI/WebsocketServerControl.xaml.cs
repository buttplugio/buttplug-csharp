using System;
using System.Windows;
using System.Windows.Controls;
using Buttplug.Core;
using ButtplugControlLibrary;
using ButtplugWebsockets;

namespace ButtplugServerGUI
{
    /// <summary>
    /// Interaction logic for WebsocketServerControl.xaml
    /// </summary>
    public partial class WebsocketServerControl : UserControl
    {
        private readonly ButtplugWebsocketServer _ws;
        private readonly ButtplugServiceFactory _bpFactory;
        private uint _port;
        private bool _secure;
        private readonly ButtplugConfig _config;

        public WebsocketServerControl(ButtplugServiceFactory bpFactory)
        {
            InitializeComponent();
            _ws = new ButtplugWebsocketServer();
            _bpFactory = bpFactory;
            _config = new ButtplugConfig("Buttplug");
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
            PortTextBox.Text = _port.ToString();
            SecureCheckBox.IsChecked = _secure;
        }

        public void StartServer()
        {
            _ws.StartServer(_bpFactory, (int) _port, _secure);
            ConnToggleButton.Content = "Stop";
            SecureCheckBox.IsEnabled = false;
            PortTextBox.IsEnabled = false;
        }

        public void StopServer()
        {
            _ws.StopServer();
            ConnToggleButton.Content = "Start";
            SecureCheckBox.IsEnabled = true;
            PortTextBox.IsEnabled = true;
        }

        private void ConnToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (ConnToggleButton.Content.Equals("Start"))
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
            if (UInt32.TryParse(PortTextBox.Text, out uint port) && port >= 1024 && port <= 65535)
            {
                _port = port;
                return;
            }
            PortTextBox.Text = _port.ToString();
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
