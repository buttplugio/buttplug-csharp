// <copyright file="ButtplugClientIPCConnectorTests.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

// Test file, disable ConfigureAwait checking.
// ReSharper disable ConsiderUsingConfigureAwait

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Client.Test;
using Buttplug.Core.Logging;
using Buttplug.Core.Test;
using Buttplug.Server.Connectors.IPCServer;
using Buttplug.Server.Test;
using NUnit.Framework;

namespace Buttplug.Client.Connectors.IPCConnector.Test
{
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Test classes can skip documentation requirements")]
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
            _ipcServer.StartServer(() =>
            {
                var server = new TestServer();
                server.AddDeviceSubtypeManager(aLogger => _subtypeMgr);
                return server;
            });
        }

        public override void SetUpConnector()
        {
            _connector = new ButtplugClientIPCConnector();
            _client = new ButtplugClient("Websocket Client", _connector);
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
            _ipcServer.Disconnect();
            await signal.WaitAsync();
        }
    }
}
