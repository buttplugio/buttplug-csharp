using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buttplug.Client.Test;
using Buttplug.Client.Connectors.IPCConnector;
using Buttplug.Components.IPCServer;
using Buttplug.Core;
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
