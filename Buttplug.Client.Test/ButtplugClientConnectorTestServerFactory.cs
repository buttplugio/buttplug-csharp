using Buttplug.Server;
using Buttplug.Server.Test;

namespace Buttplug.Client.Test
{
    public class ButtplugClientConnectorTestServerFactory : IButtplugServerFactory
    {
        private readonly TestDeviceSubtypeManager _subtypeMgr;

        public ButtplugClientConnectorTestServerFactory(TestDeviceSubtypeManager aMgr)
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