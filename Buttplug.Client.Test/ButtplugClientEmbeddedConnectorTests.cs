// <copyright file="ButtplugClientEmbeddedConnectorTests.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

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