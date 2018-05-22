using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using Buttplug.Components.Controls;
using Buttplug.Components.WebsocketServer;
using Buttplug.Core;
using Buttplug.Server;
using JetBrains.Annotations;
using Microsoft.Win32;
using Windows.UI.Notifications;

namespace Buttplug.Apps.ServerGUI
{
    /// <summary>
    /// Interaction logic for WebsocketServerControl.xaml
    /// </summary>
    public partial class ServerControl
    {
        private readonly ButtplugWebsocketServer _ws;
        private readonly IButtplugServerFactory _bpFactory;
        private readonly ButtplugConfig _config;
        private readonly ButtplugLogManager _logManager;
        private readonly IButtplugLog _log;
        private uint _port;
        private bool _secure;
        private bool _loopback;
        private string _hostname;
        private ConnUrlList _connUrls;
        private Timer _toastTimer;
        private string _currentExceptionMessage;

        public ServerControl(IButtplugServerFactory bpFactory)
        {
            InitializeComponent();
            _logManager = new ButtplugLogManager();
            _log = _logManager.GetLogger(GetType());
            _ws = new ButtplugWebsocketServer();
            _bpFactory = bpFactory;
            _config = new ButtplugConfig("Buttplug");
            _connUrls = new ConnUrlList();
            _port = 12345;

            // Usually, if we throw errors then connect, it's not actually an error. If we don't
            // connect after a second of throwing an exception, pop the toaster, but not before then.
            _toastTimer = new Timer
            {
                Interval = 1000,
                AutoReset = false,
                Enabled = false,
            };
            _toastTimer.Elapsed += PopToaster;

            if (uint.TryParse(_config.GetValue("buttplug.server.port", "12345"), out uint pres))
            {
                _port = pres;
            }

            _secure = true;
            if (bool.TryParse(_config.GetValue("buttplug.server.secure", "true"), out bool sres))
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

            _log.OnLogException += ExceptionLogged;
        }

        private void SetLastError(string aErrorMsg)
        {
            LastErrorLabel.Visibility = Visibility.Visible;
            LastError.Visibility = Visibility.Visible;
            LastError.Text = aErrorMsg;
        }

        private void ClearLastError()
        {
            LastErrorLabel.Visibility = Visibility.Hidden;
            LastError.Visibility = Visibility.Hidden;
            LastError.Text = string.Empty;
        }

        private void WebSocketExceptionHandler(object aObj, [NotNull] UnhandledExceptionEventArgs aEx)
        {
            _toastTimer.Enabled = true;
            var errorMessage = (aEx.ExceptionObject as Exception)?.Message ?? "Unknown";

            if (_secure && errorMessage.Contains("Not GET request") && _ws != null && !aEx.IsTerminating)
            {
                _log.LogException(aEx.ExceptionObject as Exception, true, errorMessage);
                return;
            }

            if (_secure && errorMessage.Contains("The handshake failed due to an unexpected packet format"))
            {
                errorMessage += "\n\nThis usually means that the client/browser tried to connect without SSL. Make sure the client is set use the wss:// URI scheme.";
            }
            else if (_secure)
            {
                errorMessage += "\n\nIf your connection is working, you can ignore this message. Otherwise, this could mean that the client/browser has not accepted our SSL certificate. Try hitting the test button on the \"Websocket Server\" tab.";
            }

            _currentExceptionMessage = errorMessage;
            _log.LogException(aEx.ExceptionObject as Exception, true, errorMessage);
        }

        private void WebSocketConnectionAccepted(object aObj, [NotNull] ConnectionEventArgs aEvent)
        {
            _toastTimer.Enabled = false;
            Dispatcher.InvokeAsync(() =>
            {
                ConnStatus.Content = "(Connected) " + aEvent.ClientName;
                DisconnectButton.IsEnabled = true;
                // We've gotten a connection, clear the last error.
                ClearLastError();
            });
        }

        private void WebSocketConnectionClosed(object aObj, [NotNull] ConnectionEventArgs aEvent)
        {
            _toastTimer.Enabled = false;
            Dispatcher.InvokeAsync(() =>
            {
                ConnStatus.Content = "(Not Connected)";
                DisconnectButton.IsEnabled = false;
            });
        }

        private void PopToaster(object aObj, ElapsedEventArgs aArgs)
        {
            _toastTimer.Enabled = false;
            Dispatcher.InvokeAsync(() =>
            {
                // Use the toast system to notify the user
                var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
                var tmp = toastXml.GetXml();
                toastXml.SelectSingleNode("//*[@id='1']").InnerText = "Buttplug Error";
                toastXml.SelectSingleNode("//*[@id='2']").InnerText = _currentExceptionMessage;
                var toast = new ToastNotification(toastXml);
                toast.Activated += OnActivatedToast;
                var appId = (string)Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\AppID\" + AppDomain.CurrentDomain.FriendlyName, "AppId",
                    string.Empty);
                if (appId != null && appId.Length > 0)
                {
                    ToastNotificationManager.CreateToastNotifier(appId).Show(toast);
                }
            });
        }

        private void ExceptionLogged(object aObj, [NotNull] LogExceptionEventArgs aEvent)
        {
            if (aEvent.ErrorMessage != null)
            {
                Dispatcher.InvokeAsync(() =>
                {
                    // Show the error message in the app
                    SetLastError(aEvent.ErrorMessage);
                });
                _toastTimer.Enabled = true;
            }
        }

        private void OnActivatedToast(ToastNotification sender, object args)
        {
            Dispatcher.Invoke(() => { Window.GetWindow(this).Activate(); });
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
                ConnInfo.IsEnabled = true;

                // We've brought the server up, clear the error.
                ClearLastError();
            }
            catch (SocketException e)
            {
                _currentExceptionMessage = e.Message;
                _log.LogException(e, true, _currentExceptionMessage);
            }
            catch (CryptographicException e)
            {
                _currentExceptionMessage =
                    "Cannot start server with SSL. Try turning off SSL. The server can still be used with ScriptPlayer, but not web applications. If you need SSL, contact Buttplug Developers for support (see About Tab).";
                _log.LogException(e, true, _currentExceptionMessage);
            }
        }

        public void StopServer()
        {
            _ws?.StopServer();
            ConnToggleButton.Content = "Start";
            SecureCheckBox.IsEnabled = true;
            PortTextBox.IsEnabled = true;
            LoopbackCheckBox.IsEnabled = true;
            ConnInfo.IsEnabled = false;
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

        private void PortTextBox_LostFocus(object sender, RoutedEventArgs e)
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
                try
                {
                    var data = (sender as Button).DataContext as ConnUrlData;
                    Clipboard.SetText(data.WsUrl);
                }
                catch (Exception ex)
                {
                    // We've seen weird instances of can't open clipboard but it's pretty rare. Log it.
                    _log.LogException(ex);
                }
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