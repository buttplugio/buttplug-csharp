using Buttplug.Core;
using Buttplug.Server;
using Buttplug.Server.Test;

namespace Buttplug.Client.Connectors.WebsocketConnector.Test
{
    public class ButtplugClientWebsocketConnectorServerFactory : IButtplugServerFactory
    {
        private readonly TestDeviceSubtypeManager _subtypeMgr;

        public ButtplugClientWebsocketConnectorServerFactory(TestDeviceSubtypeManager aMgr)
        {
            _subtypeMgr = aMgr;
        }

        public ButtplugServer GetServer()
        {
            var server = new TestServer();
            server.AddDeviceSubtypeManager(aLogger => _subtypeMgr);
            return server;
        }
    }
}