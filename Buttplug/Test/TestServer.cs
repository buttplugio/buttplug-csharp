// <copyright file="TestServer.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

// Test file, disable ConfigureAwait checking.
// ReSharper disable ConsiderUsingConfigureAwait

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Buttplug.Core.Messages;
using Buttplug.Server;
using JetBrains.Annotations;

namespace Buttplug.Test
{
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Test classes can skip documentation requirements")]
    public class TestServer : ButtplugServer
    {
        // Set MaxPingTime to zero (infinite ping/ping checks off) by default for tests
        public TestServer(uint aMaxPingTime = 0, DeviceManager aDevManager = null)
            : base("Test Server", aMaxPingTime, aDevManager)
        {
        }

        // ReSharper disable once UnusedMember.Global
        [NotNull]
        internal DeviceManager GetDeviceManager()
        {
            return _deviceManager;
        }

        internal IEnumerable<ButtplugMessage> Deserialize(string aMsg)
        {
            return _parser.Deserialize(aMsg);
        }
    }
}
