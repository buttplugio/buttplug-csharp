﻿using System;
using System.Net.Sockets;
using Buttplug.Client.Test;
using Buttplug.Components.WebsocketServer;
using Buttplug.Core;
using Buttplug.Server.Test;
using NUnit.Framework;

namespace Buttplug.Client.Connectors.WebsocketConnector.Test
{
    [TestFixture]
    public class ButtplugClientWebsocketConnectorTests : ButtplugClientConnectorTestBase
    {
        private ButtplugWebsocketServer _websocketServer;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _logMgr = new ButtplugLogManager();
            _subtypeMgr = new TestDeviceSubtypeManager(new TestDevice(_logMgr, "Test Device"));
            _websocketServer = new ButtplugWebsocketServer();
            _websocketServer.StartServer(new ButtplugClientWebsocketConnectorServerFactory(_subtypeMgr));
        }

        public override void SetUpConnector()
        {
            _connector = new ButtplugWebsocketConnector(new Uri("ws://localhost:12345/buttplug"), true);
            _client = new ButtplugClient("Websocket Client", _connector);
        }

        [Test]
        public void WrongURITest()
        {
            var wrongconnector = new ButtplugWebsocketConnector(new Uri("w://invalid:12345/buttplug"), true);
            var wrongclient = new ButtplugClient("Websocket Client", wrongconnector);
            Assert.ThrowsAsync<ArgumentException>(async () => await wrongclient.Connect());
        }

        [Test]
        public void WrongAddressTest()
        {
            var wrongconnector = new ButtplugWebsocketConnector(new Uri("ws://invalid:12345/buttplug"), true);
            var wrongclient = new ButtplugClient("Websocket Client", wrongconnector);
            Assert.ThrowsAsync<SocketException>(async () => await wrongclient.Connect());
        }
    }
}
