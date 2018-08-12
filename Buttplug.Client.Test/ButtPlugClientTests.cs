// <copyright file="ButtplugClientTests.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
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
            var testDevice = new ButtplugClientDevice(1, "Test Device", new Dictionary<string, MessageAttributes>()
            {
                { "SingleMotorVibrateCmd", new MessageAttributes() },
                { "VibrateCmd", new MessageAttributes(2) },
                { "StopDeviceCmd", new MessageAttributes() },
            });
            var testDevice2 = new ButtplugClientDevice(1, "Test Device", new Dictionary<string, MessageAttributes>()
            {
                { "SingleMotorVibrateCmd", new MessageAttributes() },
                { "VibrateCmd", new MessageAttributes(2) },
                { "StopDeviceCmd", new MessageAttributes() },
            });
            var testDevice3 = new ButtplugClientDevice(1, "Test Device", new Dictionary<string, MessageAttributes>()
            {
                { "SingleMotorVibrateCmd", new MessageAttributes() },
                { "VibrateCmd", new MessageAttributes(2) },
            });
            var testDevice4 = new ButtplugClientDevice(1, "Test Device", new Dictionary<string, MessageAttributes>()
            {
                { "SingleMotorVibrateCmd", new MessageAttributes() },
                { "VibrateCmd", new MessageAttributes(2) },
                { "DifferentName", new MessageAttributes() },
            });
            var testDevice5 = new ButtplugClientDevice(1, "Test Device", new Dictionary<string, MessageAttributes>()
            {
                { "SingleMotorVibrateCmd", new MessageAttributes() },
                { "VibrateCmd", new MessageAttributes(2) },
                { "StopDeviceCmd", new MessageAttributes() },
                { "TooMany", new MessageAttributes() },
            });

            Assert.AreEqual(testDevice, testDevice2);
            Assert.AreNotEqual(testDevice, testDevice3);
            Assert.AreNotEqual(testDevice, testDevice4);
            Assert.AreNotEqual(testDevice, testDevice5);
        }
    }
}