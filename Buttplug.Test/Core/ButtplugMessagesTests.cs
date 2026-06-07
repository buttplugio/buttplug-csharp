// <copyright file="ButtplugMessagesTests.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

// Test file, disable ConfigureAwait checking.
// ReSharper disable ConsiderUsingConfigureAwait

using System.Linq;
using Buttplug.Core.Messages;
using FluentAssertions;
using NUnit.Framework;

namespace Buttplug.Core.Test
{
    [TestFixture]
    public class ButtplugMessagesTests
    {
        private ButtplugJsonMessageParser _parser;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _parser = new ButtplugJsonMessageParser();
        }

        [Test]
        public void TestAllMessageClassesUseButtplugMessageMetadata()
        {
            foreach (var msgClass in ButtplugUtils.GetAllMessageTypes())
            {
                // This will throw if the ButtplugMessageMetadata attribute isn't present.
                ButtplugMessage.GetName(msgClass);
            }
        }

        [Test]
        public void TestStopCmdSerializesWithV4MessageName()
        {
            var msg = new StopCmd(deviceIndex: 0);

            var str = _parser.Serialize(msg);

            str.Should().Contain("\"StopCmd\"");
            str.Should().NotContain("StopDeviceCmd");
            _parser.Deserialize(str).Single().Should().BeOfType<StopCmd>();
        }

        [Test]
        public void TestStopCmdCanStopAllDevices()
        {
            var msg = new StopCmd();

            var str = _parser.Serialize(msg);

            str.Should().Contain("\"StopCmd\"");
            str.Should().NotContain("DeviceIndex");
            _parser.Deserialize(str).Single().Should().BeOfType<StopCmd>();
        }
    }
}
