// <copyright file="ButtplugClientTests.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

// Test file, disable ConfigureAwait checking.
// ReSharper disable ConsiderUsingConfigureAwait

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using FluentAssertions;
using NUnit.Framework;

namespace Buttplug.Client.Test
{
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Test classes can skip documentation requirements")]
    [TestFixture]
    public class ButtplugClientTests
    {
        [Test]
        public void TestClientDeviceEquality()
        {
            var logMgr = new ButtplugLogManager();
            var client = new ButtplugClient("Test Device Client", new ButtplugEmbeddedConnector("Test Device Server"));
            Task SendFunc(ButtplugClientDevice device, ButtplugMessage msg, CancellationToken token) => Task.CompletedTask;
            var testDevice = new ButtplugClientDevice(logMgr, client, SendFunc, 1, "Test Device", new Dictionary<string, MessageAttributes>()
            {
                { "SingleMotorVibrateCmd", new MessageAttributes() },
                { "VibrateCmd", new MessageAttributes(2) },
                { "StopDeviceCmd", new MessageAttributes() },
            });
            var testDevice2 = new ButtplugClientDevice(logMgr, client, SendFunc, 1, "Test Device", new Dictionary<string, MessageAttributes>()
            {
                { "SingleMotorVibrateCmd", new MessageAttributes() },
                { "VibrateCmd", new MessageAttributes(2) },
                { "StopDeviceCmd", new MessageAttributes() },
            });
            var testDevice3 = new ButtplugClientDevice(logMgr, client, SendFunc, 1, "Test Device", new Dictionary<string, MessageAttributes>()
            {
                { "SingleMotorVibrateCmd", new MessageAttributes() },
                { "VibrateCmd", new MessageAttributes(2) },
            });
            var testDevice4 = new ButtplugClientDevice(logMgr, client, SendFunc, 1, "Test Device", new Dictionary<string, MessageAttributes>()
            {
                { "SingleMotorVibrateCmd", new MessageAttributes() },
                { "VibrateCmd", new MessageAttributes(2) },
                { "FleshlightLaunchFW12Cmd", new MessageAttributes() },
            });
            var testDevice5 = new ButtplugClientDevice(logMgr, client, SendFunc, 1, "Test Device", new Dictionary<string, MessageAttributes>()
            {
                { "SingleMotorVibrateCmd", new MessageAttributes() },
                { "VibrateCmd", new MessageAttributes(2) },
                { "StopDeviceCmd", new MessageAttributes() },
                { "RotateCmd", new MessageAttributes(1) },
            });

            var newClient = new ButtplugClient("Other Test Device Client", new ButtplugEmbeddedConnector("Other Test Device Server"));
            var otherTestDevice = new ButtplugClientDevice(logMgr, newClient, SendFunc, 1, "Test Device", new Dictionary<string, MessageAttributes>()
            {
                { "SingleMotorVibrateCmd", new MessageAttributes() },
                { "VibrateCmd", new MessageAttributes(2) },
                { "StopDeviceCmd", new MessageAttributes() },
            });

            testDevice.Should().BeEquivalentTo(testDevice2);
            testDevice.Should().NotBe(testDevice3);
            testDevice.Should().NotBe(testDevice4);
            testDevice.Should().NotBe(testDevice5);
            testDevice.Should().NotBe(otherTestDevice);
        }
    }
}