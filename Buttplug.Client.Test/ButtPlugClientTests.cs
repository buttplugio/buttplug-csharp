// <copyright file="ButtplugClientTests.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Messages;
using NUnit.Framework;

namespace Buttplug.Client.Test
{
    [TestFixture]
    public class ButtplugClientTests
    {
        [Test]
        public void TestClientDeviceEquality()
        {
            var client = new ButtplugClient("Test Device Client", new ButtplugEmbeddedConnector("Test Device Server"));
            Task SendFunc(ButtplugClientDevice device, ButtplugMessage msg, CancellationToken token) => Task.CompletedTask;
            var testDevice = new ButtplugClientDevice(client, SendFunc, 1, "Test Device", new Dictionary<string, MessageAttributes>()
            {
                { "SingleMotorVibrateCmd", new MessageAttributes() },
                { "VibrateCmd", new MessageAttributes(2) },
                { "StopDeviceCmd", new MessageAttributes() },
            });
            var testDevice2 = new ButtplugClientDevice(client, SendFunc, 1, "Test Device", new Dictionary<string, MessageAttributes>()
            {
                { "SingleMotorVibrateCmd", new MessageAttributes() },
                { "VibrateCmd", new MessageAttributes(2) },
                { "StopDeviceCmd", new MessageAttributes() },
            });
            var testDevice3 = new ButtplugClientDevice(client, SendFunc, 1, "Test Device", new Dictionary<string, MessageAttributes>()
            {
                { "SingleMotorVibrateCmd", new MessageAttributes() },
                { "VibrateCmd", new MessageAttributes(2) },
            });
            var testDevice4 = new ButtplugClientDevice(client, SendFunc, 1, "Test Device", new Dictionary<string, MessageAttributes>()
            {
                { "SingleMotorVibrateCmd", new MessageAttributes() },
                { "VibrateCmd", new MessageAttributes(2) },
                { "DifferentName", new MessageAttributes() },
            });
            var testDevice5 = new ButtplugClientDevice(client, SendFunc, 1, "Test Device", new Dictionary<string, MessageAttributes>()
            {
                { "SingleMotorVibrateCmd", new MessageAttributes() },
                { "VibrateCmd", new MessageAttributes(2) },
                { "StopDeviceCmd", new MessageAttributes() },
                { "TooMany", new MessageAttributes() },
            });

            var newClient = new ButtplugClient("Other Test Device Client", new ButtplugEmbeddedConnector("Other Test Device Server"));
            var otherTestDevice = new ButtplugClientDevice(newClient, SendFunc, 1, "Test Device", new Dictionary<string, MessageAttributes>()
            {
                { "SingleMotorVibrateCmd", new MessageAttributes() },
                { "VibrateCmd", new MessageAttributes(2) },
                { "StopDeviceCmd", new MessageAttributes() },
            });

            Assert.AreEqual(testDevice, testDevice2);
            Assert.AreNotEqual(testDevice, testDevice3);
            Assert.AreNotEqual(testDevice, testDevice4);
            Assert.AreNotEqual(testDevice, testDevice5);
            Assert.AreNotEqual(testDevice, otherTestDevice);
        }
    }
}