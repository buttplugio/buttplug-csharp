// <copyright file="WebsocketServerTest.cs" company="Nonpolynomial Labs LLC">
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
using Buttplug.Client;
using Buttplug.Client.Connectors.WebsocketConnector;
using FluentAssertions;
using NUnit.Framework;

namespace Buttplug.Server.Connectors.WebsocketServer.Test
{
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Test classes can skip documentation requirements")]
    [TestFixture]
    public class WebsocketServerTest
    {
        private ButtplugClient _client;
        private ButtplugWebsocketConnector _connector;
        private ButtplugWebsocketServer _server;

        [SetUp]
        public void SetUp()
        {
            _connector = new ButtplugWebsocketConnector(new Uri("ws://localhost:12345"));
            _client = new ButtplugClient("Test Client", _connector);
            _server = new ButtplugWebsocketServer();
        }

        [TearDown]
        public async Task TearDown()
        {
            await _server.StopServerAsync();
        }

        // Test that ConnectionAccepted is fired after client finished handshake.
        [Test]
        public async Task TestConnectionAcceptedEvent()
        {
            // There's a good chance the server will fire ConnectionAccepted while we're calling
            // ConnectAsync(), but using a semaphore means we'll pass even if its out of sync.
            var sem = new SemaphoreSlim(0, 1);
            _server.ConnectionAccepted += (aObject, aEventArgs) => { sem.Release(); };
            await _server.StartServerAsync(() => new ButtplugServer("Test Server", 0));
            sem.CurrentCount.Should().Be(0);
            await _client.ConnectAsync();
            await sem.WaitAsync();
        }
        
        // Test that ConnectionClosed is fired when client closes connection.
        [Test]
        public async Task TestConnectionClosedEvent()
        {
            // We won't get ConnectionClosed until the websocket is completely closed, which
            // sometimes happens after DisconnectionAsync() completes. Use the semaphore here to wait
            // on that.
            var sem = new SemaphoreSlim(0, 1);
            _server.ConnectionClosed += (aObject, aEventArgs) => { sem.Release(); };
            await _server.StartServerAsync(() => new ButtplugServer("Test Server", 0));
            await _client.ConnectAsync();
            // Make sure nothing got fired after connect
            sem.CurrentCount.Should().Be(0);
            await _client.DisconnectAsync();
            await sem.WaitAsync();
        }

        /*
        // If a client does not respond with RequestServerInfo in a specified amount of time, a
        // connection should be dropped.
        [Test]
        public async Task TestDropConnectionOnRequestServerInfoTimeout()
        {

        }

        // Test Ping Disconnect
        [Test]
        public async Task TestDropConnectionOnPingTimeout()
        {

        }
        */
    }
}
