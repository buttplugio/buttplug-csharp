// <copyright file="ButtplugClientConnectorTestServerFactory.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using Buttplug.Server;
using Buttplug.Server.Test;

namespace Buttplug.Client.Test
{
    public class ButtplugClientConnectorTestServerFactory : IButtplugServerFactory
    {
        private readonly TestDeviceSubtypeManager _subtypeMgr;

        public ButtplugClientConnectorTestServerFactory(TestDeviceSubtypeManager aMgr)
        {
            _subtypeMgr = aMgr;
        }

        public ButtplugServer GetServer()
        {
            var server = new TestServer();
            server.AddDeviceSubtypeManager(aLogger => _subtypeMgr);
            return server;
        }
    }
}