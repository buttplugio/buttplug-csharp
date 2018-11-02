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
using JetBrains.Annotations;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace Buttplug.Server.Test
{
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Test classes can skip documentation requirements")]
    public class TestServer : ButtplugServer
    {
        // Set MaxPingTime to zero (infinite ping/ping checks off) by default for tests
        public TestServer(uint aMaxPingTime = 0, DeviceManager aDevManager = null)
            : base("Test Server", aMaxPingTime, aDevManager)
        {
            // Build ourselves an NLog manager just so we can see what's going on.
            var dt = new DebuggerTarget();
            LogManager.Configuration = new LoggingConfiguration();
            LogManager.Configuration.AddTarget("debugger", dt);
            LogManager.Configuration.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, dt));
            LogManager.Configuration = LogManager.Configuration;
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
