using Buttplug.Client.Test;
using Buttplug.Core;
using Buttplug.Core.Test;
using Buttplug.Server.Connectors.IPCServer;
using Buttplug.Server.Test;
using NUnit.Framework;

namespace Buttplug.Client.Connectors.IPCConnector.Test
{ 
    [TestFixture]
    public class ButtplugClientIPCConnectorTests : ButtplugClientConnectorTestBase
    {
        private ButtplugIPCServer _ipcServer;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _logMgr = new ButtplugLogManager();
            _subtypeMgr = new TestDeviceSubtypeManager(new TestDevice(_logMgr, "Test Device"));
            _ipcServer = new ButtplugIPCServer();
            _ipcServer.StartServer(new ButtplugClientConnectorTestServerFactory(_subtypeMgr));
        }

        public override void SetUpConnector()
        {
            _connector = new ButtplugClientIPCConnector();
            _client = new ButtplugClient("Websocket Client", _connector);
        }
    }
}
