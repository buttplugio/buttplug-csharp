using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using Buttplug.Components.WebsocketServer;
using Buttplug.Server;
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
        private bool _loopback;
        private string _hostname;
        private ConnUrlList _connUrls;

        public WebsocketServerControl(IButtplugServerFactory bpFactory)
        {
            InitializeComponent();
            _ws = new ButtplugWebsocketServer();
            _bpFactory = bpFactory;
            _config = new ButtplugConfig("Buttplug");
            _connUrls = new ConnUrlList();
            _port = 12345;
            if (uint.TryParse(_config.GetValue("buttplug.server.port", "12345"), out uint pres))
            {
                _port = pres;
            }

            _secure = false;
            if (bool.TryParse(_config.GetValue("buttplug.server.secure", "false"), out bool sres))
            {
                _secure = sres;
            }

            _loopback = true;
            if (bool.TryParse(_config.GetValue("buttplug.server.loopbackOnly", "true"), out bool lres))
            {
                _loopback = lres;
            }

            _hostname = _config.GetValue("buttplug.server.hostname", "localhost");

            PortTextBox.Text = _port.ToString();
            SecureCheckBox.IsChecked = _secure;
            LoopbackCheckBox.IsChecked = _loopback;

            ConnectionUrl.ItemsSource = _connUrls;

            _ws.OnException += WebSocketExceptionHandler;
            _ws.ConnectionAccepted += WebSocketConnectionAccepted;
            _ws.ConnectionUpdated += WebSocketConnectionAccepted;
            _ws.ConnectionClosed += WebSocketConnectionClosed;
        }

        private void WebSocketExceptionHandler(object aObj, [NotNull] UnhandledExceptionEventArgs aEx)
        {
            var log = LogManager.GetCurrentClassLogger();
            log.Error("Exception of type " + aEx.ExceptionObject.GetType() + " encountered: " + (aEx.ExceptionObject as Exception)?.Message);
            MessageBox.Show((aEx.ExceptionObject as Exception)?.Message ?? "Unknown", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }

        private void WebSocketConnectionAccepted(object aObj, [NotNull] ConnectionEventArgs aEvent)
        {
            Dispatcher.InvokeAsync(() =>
            {
                ConnStatus.Content = "(Connected) " + aEvent.ClientName;
                DisconnectButton.IsEnabled = true;
            });
        }

        private void WebSocketConnectionClosed(object aObj, [NotNull] ConnectionEventArgs aEvent)
        {
            Dispatcher.InvokeAsync(() =>
            {
                ConnStatus.Content = "(Not Connected)";
                DisconnectButton.IsEnabled = false;
            });
        }

        public void StartServer()
        {
            try
            {
                _ws.StartServer(_bpFactory, (int)_port, _loopback, _secure, _hostname);
                ConnToggleButton.Content = "Stop";
                SecureCheckBox.IsEnabled = false;
                PortTextBox.IsEnabled = false;
                LoopbackCheckBox.IsEnabled = false;
                _connUrls.Clear();
                _connUrls.Add(new ConnUrlData(_secure, "localhost", _port));

                if (!_loopback)
                {
                    foreach (var network in NetworkInterface.GetAllNetworkInterfaces())
                    {
                        foreach (IPAddressInformation address in network.GetIPProperties().UnicastAddresses)
                        {
                            if (address.Address.AddressFamily == AddressFamily.InterNetwork &&
                                !IPAddress.IsLoopback(address.Address))
                            {
                                _connUrls.Add(new ConnUrlData(_secure, address.Address.ToString(), _port));
                            }
                        }
                    }
                }

                ConnStatus.Content = "(Not Connected)";
                DisconnectButton.IsEnabled = false;
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
            LoopbackCheckBox.IsEnabled = true;
            ConnInfo.Visibility = Visibility.Collapsed;
        }

        private void ConnToggleButton_Click(object aObj, RoutedEventArgs aEvent)
        {
            if (ConnToggleButton.Content.Equals("Start"))
            {
                StartServer();
                _config.SetValue("buttplug.server.port", _port.ToString());
                _config.SetValue("buttplug.server.secure", _secure.ToString());
                _config.SetValue("buttplug.server.loopbackOnly", _loopback.ToString());
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

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            _ws.Disconnect();
        }

        private void LoopbackCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _loopback = false;
        }

        private void LoopbackCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _loopback = true;
        }

        private void ConnItemCopy_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button)
            {
                var data = (sender as Button).DataContext as ConnUrlData;
                Clipboard.SetText(data.WsUrl);
            }
        }

        private void ConnItemTest_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button)
            {
                var data = (sender as Button).DataContext as ConnUrlData;
                System.Diagnostics.Process.Start(new Uri(data.TestUrl).AbsoluteUri);
            }
        }
    }

    public class ConnUrlList : ObservableCollection<ConnUrlData>
    {
    }

    public class ConnUrlData
    {
        public string WsUrl;
        public string TestUrl;

        public ConnUrlData(bool aSecure, string aHost, uint aPort)
        {
            WsUrl = (aSecure ? "wss" : "ws") + "://" + aHost + ":" + aPort.ToString() + "/buttplug";
            TestUrl = (aSecure ? "https" : "http") + "://" + aHost + ":" + aPort.ToString() + "/";
        }

        public override string ToString()
        {
            return WsUrl;
        }
    }
}
