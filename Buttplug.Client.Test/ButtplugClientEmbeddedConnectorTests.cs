using Buttplug.Server;
using Buttplug.Server.Test;
using NUnit.Framework;

namespace Buttplug.Client.Test
{
    [TestFixture]
    public class ButtplugClientEmbeddedConnectorTests : ButtplugClientConnectorTestBase
    {
        internal ButtplugServer _server;

        public override void SetUpConnector()
        {
            _subtypeMgr = new TestDeviceSubtypeManager(new TestDevice(_logMgr, "Test Device"));
            _server = new TestServer();
            _server.AddDeviceSubtypeManager(_subtypeMgr);
            _connector = new ButtplugEmbeddedConnector(_server, "Test Server", 100);
            _client = new ButtplugClient("Test Client", _connector);
        }
    }
}