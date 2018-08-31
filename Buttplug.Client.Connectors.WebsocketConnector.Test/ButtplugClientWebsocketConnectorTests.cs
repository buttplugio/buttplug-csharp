using System;
using System.Threading.Tasks;
using Buttplug.Client.Test;
using Buttplug.Core;
using Buttplug.Core.Logging;
using Buttplug.Core.Test;
using Buttplug.Server.Connectors.WebsocketServer;
using Buttplug.Server.Test;
using NUnit.Framework;

namespace Buttplug.Client.Connectors.WebsocketConnector.Test
{
    [TestFixture]
    public class ButtplugClientWebsocketConnectorTests : ButtplugClientConnectorTestBase
    {
        private ButtplugWebsocketServer _websocketServer;

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            _logMgr = new ButtplugLogManager();
            _subtypeMgr = new TestDeviceSubtypeManager(new TestDevice(_logMgr, "Test Device"));
            _websocketServer = new ButtplugWebsocketServer();
            await _websocketServer.StartServer(() =>
            {
                var server = new TestServer();
                server.AddDeviceSubtypeManager(aLogger => _subtypeMgr);
                return server;
            });
        }

        public override void SetUpConnector()
        {
            _connector = new ButtplugWebsocketConnector(new Uri("ws://localhost:12345/buttplug"));
            _client = new ButtplugClient("Websocket Client", _connector);
        }

        [Test]
        public void WrongURITest()
        {
            var wrongconnector = new ButtplugWebsocketConnector(new Uri("w://invalid:12345/buttplug"));
            var wrongclient = new ButtplugClient("Websocket Client", wrongconnector);
            Assert.ThrowsAsync<ButtplugClientConnectorException>(async () => await wrongclient.ConnectAsync());
        }

        [Test]
        public void WrongAddressTest()
        {
            var wrongconnector = new ButtplugWebsocketConnector(new Uri("ws://invalid:12345/buttplug"));
            var wrongclient = new ButtplugClient("Websocket Client", wrongconnector);
            Assert.ThrowsAsync<ButtplugClientConnectorException>(async () => await wrongclient.ConnectAsync());
        }
    }
}
