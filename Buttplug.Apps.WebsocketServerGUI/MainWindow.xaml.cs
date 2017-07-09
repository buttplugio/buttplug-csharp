using System.ComponentModel;

namespace Buttplug.Apps.WebsocketServerGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly WebsocketServerControl _wsTab;

        public MainWindow()
        {
            var config = new ButtplugConfig("Buttplug");
            uint ping;
            uint.TryParse(config.GetValue("buttplug.server.maxPing", "100"), out ping);

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
