// <copyright file="ButtplugClientConnectorTestBase.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

// Test file, disable ConfigureAwait checking.
// ReSharper disable ConsiderUsingConfigureAwait

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Client.Test;
using Buttplug.Core.Logging;
using Buttplug.Core.Test;
using Buttplug.Server.Connectors.WebsocketServer;
using Buttplug.Server.Test;
using FluentAssertions;
using NUnit.Framework;

namespace Buttplug.Client.Connectors.WebsocketConnector.Test
{
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Test classes can skip documentation requirements")]
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
            await _websocketServer.StartServerAsync(() =>
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
        public void TestWrongURI()
        {
            var wrongConnector = new ButtplugWebsocketConnector(new Uri("w://invalid:12345/buttplug"));
            var wrongClient = new ButtplugClient("Websocket Client", wrongConnector);
            wrongClient.Awaiting(async aClient => await aClient.ConnectAsync()).Should()
                .Throw<ButtplugClientConnectorException>();
        }

        [Test]
        public void TestWrongAddress()
        {
            var wrongConnector = new ButtplugWebsocketConnector(new Uri("ws://invalid:12345/buttplug"));
            var wrongClient = new ButtplugClient("Websocket Client", wrongConnector);
            wrongClient.Awaiting(async aClient => await aClient.ConnectAsync()).Should()
                .Throw<ButtplugClientConnectorException>();
        }

        [Test]
        public async Task TestServerDisconnect()
        {
            SetUpConnector();
            var signal = new SemaphoreSlim(0, 1);
            _client.ServerDisconnect += (aObj, aEventArgs) =>
            {
                if (signal.CurrentCount == 0)
                {
                    signal.Release(1);
                }
            };
            await _client.ConnectAsync();
            await _websocketServer.DisconnectAsync();
            await signal.WaitAsync();
        }
    }
}
