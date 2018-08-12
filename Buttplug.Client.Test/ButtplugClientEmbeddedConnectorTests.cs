using Buttplug.Core;
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
            // This is a test, so just ignore the logger requirement for now.
            _server.AddDeviceSubtypeManager(aLog => _subtypeMgr);
            _connector = new ButtplugEmbeddedConnector(_server);
            _client = new ButtplugClient("Test Client", _connector);
        }
    }
}