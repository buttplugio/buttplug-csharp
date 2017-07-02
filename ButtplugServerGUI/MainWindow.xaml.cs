using System;
using System.ComponentModel;

namespace ButtplugServerGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        WebsocketServerControl _wsTab;

        public MainWindow()
        {
            var config = new ButtplugConfig("Buttplug");
            uint ping = 100;
            UInt32.TryParse(config.GetValue("buttplug.server.maxPing", "100"), out ping);

            InitializeComponent();

            ButtplugTab.SetServerDetails("Websocket Server", ping);
            _wsTab = new WebsocketServerControl(ButtplugTab);
            ButtplugTab.SetApplicationTab("Websocket Server", _wsTab);
            Closing += ClosingHandler;
            _wsTab.StartServer();
        }

        private void ClosingHandler(object aObj, CancelEventArgs e)
        {
            _wsTab?.StopServer();
        }
    }
}
