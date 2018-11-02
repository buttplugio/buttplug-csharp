// <copyright file="ButtplugClientEmbeddedConnectorTests.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

// Test file, disable ConfigureAwait checking.
// ReSharper disable ConsiderUsingConfigureAwait

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Test;
using Buttplug.Server;
using Buttplug.Server.Test;
using NUnit.Framework;

namespace Buttplug.Client.Test
{
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Test classes can skip documentation requirements")]
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

        private class ButtplugNoPingTestClient : ButtplugClient
        {
            public ButtplugNoPingTestClient(IButtplugClientConnector aConnector)
                : base("TestClient", aConnector)
            {
            }

            public new async Task ConnectAsync(CancellationToken aToken = default(CancellationToken))
            {
                await base.ConnectAsync(aToken);

                // Run connection, then just get rid of the ping timer.
                _pingTimer.Dispose();
            }
        }

        [Test]
        public async Task TestClientPingTimeout()
        {
            _server = new TestServer(50);
            var signal = new SemaphoreSlim(1, 1);

            // This is a test, so just ignore the logger requirement for now.
            _connector = new ButtplugEmbeddedConnector(_server);
            _client = new ButtplugNoPingTestClient(_connector);
            _client.PingTimeout += (aObj, aEventArgs) =>
            {
                if (signal.CurrentCount == 0)
                {
                    signal.Release(1);
                }
            };

            // We should connect, then basically be instantly disconnected due to ping timeout
            await _client.ConnectAsync();
            await signal.WaitAsync();
        }

        // Connectors using the MessageSorter will overwrite message IDs, so this requires using an
        // embedded server connector.
        [Test]
        public async Task TestSendSystemIdMessage()
        {
            var c = new SystemMessageSendingClient("TestClient", _connector);
            await c.ConnectAsync();

            // For some reason, trying this with FluentAssertions Awaiting clauses causes a stall. Back to Asserts.
            try
            {
                await c.SendSystemIdMessage();
                Assert.Fail("Should throw!");
            }
            catch (ButtplugMessageException)
            {
                Assert.Pass("Got expected exception");
            }
        }
    }
}