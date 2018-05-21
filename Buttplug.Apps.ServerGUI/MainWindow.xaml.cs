using System.ComponentModel;
using Buttplug.Components.Controls;

namespace Buttplug.Apps.ServerGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly ServerControl _wsTab;

        public MainWindow()
        {
            var config = new ButtplugConfig("Buttplug");
            uint ping = 0;

            // As long as the server is websocket only, we can depend on websocket ping as an
            // application keepalive. Once we support both IPC and WebSocket, we should implement
            // required ping for IPC.

            // uint.TryParse(config.GetValue("buttplug.server.maxPing", "1000"), out ping);

            InitializeComponent();

            long logLimit = 1000;
            if (long.TryParse(config.GetValue("buttplug.log.max", "1000"), out long res))
            {
                logLimit = res;
            }

            ButtplugTab.GetLogControl().MaxLogs = logLimit;

            ButtplugTab.SetServerDetails("Websocket Server", ping);
            _wsTab = new ServerControl(ButtplugTab);
            ButtplugTab.SetApplicationTab("Websocket Server", _wsTab);

            ButtplugTab.GetAboutControl().CheckUpdate(config, "buttplug-csharp");
            Closing += ClosingHandler;
            _wsTab.StartServer();
        }

        private void ClosingHandler(object aObj, CancelEventArgs e)
        {
            _wsTab?.StopServer();
        }
    }
}
