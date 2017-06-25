using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Buttplug.Core;
using ButtplugWebsockets;

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

        public WebsocketServerControl(ButtplugService aService)
        {
            InitializeComponent();
            _ws = new ButtplugWebsocketServer();
            _service = aService;
            _port = 12345;
            _secure = false;
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

            if (((Button)ConnToggleButton).Content == "Start")
            {
                StartServer();
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
