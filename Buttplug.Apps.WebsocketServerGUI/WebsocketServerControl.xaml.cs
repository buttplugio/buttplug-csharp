using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using Buttplug.Components.WebsocketServer;
using Buttplug.Server;
using System;
using JetBrains.Annotations;
using NLog;

namespace Buttplug.Apps.WebsocketServerGUI
{
    /// <summary>
    /// Interaction logic for WebsocketServerControl.xaml
    /// </summary>
    public partial class WebsocketServerControl
    {
        private readonly ButtplugWebsocketServer _ws;
        private readonly IButtplugServerFactory _bpFactory;
        private readonly ButtplugConfig _config;
        private uint _port;
        private bool _secure;

        public WebsocketServerControl(IButtplugServerFactory bpFactory)
        {
            InitializeComponent();
            _ws = new ButtplugWebsocketServer();
            _bpFactory = bpFactory;
            _config = new ButtplugConfig("Buttplug");
            _port = 12345;
            _secure = false;
            if (uint.TryParse(_config.GetValue("buttplug.server.port", "12345"), out uint pres))
            {
                _port = pres;
            }

            if (bool.TryParse(_config.GetValue("buttplug.server.secure", "false"), out bool sres))
            {
                _secure = sres;
            }

            PortTextBox.Text = _port.ToString();
            SecureCheckBox.IsChecked = _secure;

            _ws.OnException += WebSocketExceptionHandler;
        }

        private void WebSocketExceptionHandler(object aObj, [NotNull] UnhandledExceptionEventArgs aEx)
        {
            var log = LogManager.GetCurrentClassLogger();
            log.Error("Exception of type " + aEx.ExceptionObject.GetType() + " encountered: " + (aEx.ExceptionObject as Exception)?.Message);
            MessageBox.Show((aEx.ExceptionObject as Exception)?.Message ?? "Unknown", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }

        public void StartServer()
        {
            try
            {
                _ws.StartServer(_bpFactory, (int)_port, _secure);
                ConnToggleButton.Content = "Stop";
                SecureCheckBox.IsEnabled = false;
                PortTextBox.IsEnabled = false;
                ConnectionUrl.Text = (_secure ? "wss" : "ws") + "://localhost:" + _port.ToString() + "/buttplug";
                TestUrl.Inlines.Clear();
                TestUrl.Inlines.Add((_secure ? "https" : "http") + "://localhost:" + _port.ToString());
                ConnInfo.Visibility = Visibility.Visible;
            }
            catch (SocketException e)
            {
                MessageBox.Show(e.Message, "Buttplug Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        public void StopServer()
        {
            _ws.StopServer();
            ConnToggleButton.Content = "Start";
            SecureCheckBox.IsEnabled = true;
            PortTextBox.IsEnabled = true;
            ConnInfo.Visibility = Visibility.Collapsed;
        }

        private void ConnToggleButton_Click(object aObj, RoutedEventArgs aEvent)
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

        private void PortTextBox_TextChanged(object aObj, TextChangedEventArgs aEvent)
        {
            if (uint.TryParse(PortTextBox.Text, out uint port) && port >= 1024 && port <= 65535)
            {
                _port = port;
                return;
            }

            PortTextBox.Text = _port.ToString();
        }

        private void SecureCheckBox_Unchecked(object aObj, RoutedEventArgs aEvent)
        {
            _secure = false;
        }

        private void SecureCheckBox_Checked(object aObj, RoutedEventArgs aEvent)
        {
            _secure = true;
        }

        private void TestUrl_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(new Uri((_secure ? "https" : "http") + "://localhost:" + _port.ToString()).AbsoluteUri);
        }
    }
}
