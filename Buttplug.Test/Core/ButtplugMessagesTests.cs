﻿// <copyright file="ButtplugMessagesTests.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

// Test file, disable ConfigureAwait checking.
// ReSharper disable ConsiderUsingConfigureAwait

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using FluentAssertions;
using NUnit.Framework;

namespace Buttplug.Core.Test
{
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Test classes can skip documentation requirements")]
    [TestFixture]
    public class ButtplugMessagesTests
    {
        private ButtplugLogManager _logManager;
        private ButtplugJsonMessageParser _parser;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _logManager = new ButtplugLogManager();
            _parser = new ButtplugJsonMessageParser(_logManager);
        }

        private T CheckParsedVersion<T>(ButtplugMessage msg, uint schemversion, string jsonStr)
            where T : ButtplugMessage
        {
            var str = _parser.Serialize(msg, schemversion);
            str.Should().Be(jsonStr);
            var msgs = _parser.Deserialize(str).ToArray();
            msgs.Length.Should().Be(1);
            var msg = msgs[0];
            msg.Should().BeOfType<T>();
            return msg as T;
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
        public void TestLovenseCmd()
        {
            void CheckMsg(LovenseCmd msg)
            {
                msg.DeviceIndex.Should().Be(2);
                msg.Id.Should().Be(4);
                msg.Command.Should().Be("Vibrate:2;");
            }

            var origMsg = new LovenseCmd(2, "Vibrate:2;", 4);
            CheckMsg(origMsg);
            var msg = CheckParsedVersion<LovenseCmd>(origMsg, 0, "[{\"LovenseCmd\":{\"Command\":\"Vibrate:2;\",\"DeviceIndex\":2,\"Id\":4}}]");
            CheckMsg(msg);
        }

        [Test]
        public void TestDeviceAddedCmdVersion1()
        {
            void CheckMsg(DeviceAdded msg)
            {
                msg.DeviceIndex.Should().Be(2);
                msg.Id.Should().Be(0);
                msg.DeviceName.Should().Be("testDev");
                msg.DeviceMessages.Count.Should().Be(2);
                msg.DeviceMessages.Keys.Should().Contain(new[] { "StopDeviceCmd", "VibrateCmd" });
                msg.DeviceMessages["VibrateCmd"].FeatureCount.Should().Be(1);
            }

            var msg = new DeviceAdded(2, "testDev", new Dictionary<string, MessageAttributes>
            {
                { "StopDeviceCmd", new MessageAttributes() },
                { "VibrateCmd", new MessageAttributes() { FeatureCount = 1 } },
            });

            CheckMsg(msg);
            var msgSchemv1 = CheckParsedVersion<DeviceAdded>(msg, 1,
                "[{\"DeviceAdded\":{\"DeviceName\":\"testDev\",\"DeviceMessages\":{\"StopDeviceCmd\":{},\"VibrateCmd\":{\"FeatureCount\":1}},\"DeviceIndex\":2,\"Id\":0}}]");
            CheckMsg(msgSchemv1);
        }

        [Test]
        public void TestDeviceAddedCmdVersion0()
        {
            void CheckMsg(DeviceAddedVersion0 msg)
            {
                msg.DeviceIndex.Should().Be(2);
                msg.Id.Should().Be(0);
                msg.DeviceName.Should().Be("testDev");
                msg.DeviceMessages.Length.Should().Be(2);
                msg.DeviceMessages.Should().Contain(new[] { "StopDeviceCmd", "VibrateCmd" });
            }

            var msg = new DeviceAdded(2, "testDev", new Dictionary<string, MessageAttributes>
            {
                { "StopDeviceCmd", new MessageAttributes() },
                { "VibrateCmd", new MessageAttributes() { FeatureCount = 1 } },
            });

            var msgSchemv0 = CheckParsedVersion<DeviceAddedVersion0>(msg, 0,
                "[{\"DeviceAdded\":{\"DeviceName\":\"testDev\",\"DeviceMessages\":[\"StopDeviceCmd\",\"VibrateCmd\"],\"DeviceIndex\":2,\"Id\":0}}]");
            CheckMsg(msgSchemv0);
        }

        [Test]
        public void TestDeviceListCmdVersion1()
        {
            void CheckMsg(DeviceList msg)
            {
                msg.Id.Should().Be(6);
                msg.Devices.Length.Should().Be(2);
                msg.Devices[0].DeviceName.Should().Be("testDev0");
                msg.Devices[0].DeviceIndex.Should().Be(2);
                msg.Devices[0].DeviceMessages.Count.Should().Be(2);
                msg.Devices[0].DeviceMessages.Should().ContainKeys("StopDeviceCmd", "VibrateCmd");
                msg.Devices[0].DeviceMessages["StopDeviceCmd"].FeatureCount.Should().BeNull();
                msg.Devices[0].DeviceMessages["VibrateCmd"].FeatureCount.Should().Be(1);

                msg.Devices[1].DeviceName.Should().Be("testDev1");
                msg.Devices[1].DeviceIndex.Should().Be(5);
                msg.Devices[1].DeviceMessages.Count.Should().Be(2);
                msg.Devices[1].DeviceMessages.Should().ContainKeys("StopDeviceCmd", "RotateCmd");
                msg.Devices[1].DeviceMessages["StopDeviceCmd"].FeatureCount.Should().BeNull();
                msg.Devices[1].DeviceMessages["RotateCmd"].FeatureCount.Should().Be(2);
            }

            var msg = new DeviceList(new[]
            {
                new DeviceMessageInfo(2, "testDev0", new Dictionary<string, MessageAttributes>
                {
                    { "StopDeviceCmd", new MessageAttributes() },
                    { "VibrateCmd", new MessageAttributes() { FeatureCount = 1 } },
                }),
                new DeviceMessageInfo(5, "testDev1", new Dictionary<string, MessageAttributes>
                {
                    { "StopDeviceCmd", new MessageAttributes() },
                    { "RotateCmd", new MessageAttributes() { FeatureCount = 2 } },
                }),
            }, 6);

            CheckMsg(msg);

            var newMsg = CheckParsedVersion<DeviceList>(msg, 1,
                "[{\"DeviceList\":{\"Devices\":[{\"DeviceName\":\"testDev0\",\"DeviceIndex\":2,\"DeviceMessages\":{\"StopDeviceCmd\":{},\"VibrateCmd\":{\"FeatureCount\":1}}},{\"DeviceName\":\"testDev1\",\"DeviceIndex\":5,\"DeviceMessages\":{\"StopDeviceCmd\":{},\"RotateCmd\":{\"FeatureCount\":2}}}],\"Id\":6}}]"
            );

            CheckMsg(newMsg);
        }

        [Test]
        public void TestDeviceListCmdVersion0()
        {
            void CheckMsg(DeviceListVersion0 msg)
            {
                msg.Id.Should().Be(6);
                msg.Devices.Length.Should().Be(2);
                msg.Devices[0].DeviceName.Should().Be("testDev0");
                msg.Devices[0].DeviceIndex.Should().Be(2);
                msg.Devices[0].DeviceMessages.Length.Should().Be(2);
                msg.Devices[0].DeviceMessages.Should().Contain("StopDeviceCmd", "VibrateCmd");

                msg.Devices[1].DeviceName.Should().Be("testDev1");
                msg.Devices[1].DeviceIndex.Should().Be(5);
                msg.Devices[1].DeviceMessages.Length.Should().Be(2);
                msg.Devices[1].DeviceMessages.Should().Contain("StopDeviceCmd", "RotateCmd");
            }

            var msg = new DeviceListVersion0(new[]
            {
                new DeviceMessageInfoVersion0(2, "testDev0", new[] { "StopDeviceCmd", "VibrateCmd" }),
                new DeviceMessageInfoVersion0(5, "testDev1", new[] { "StopDeviceCmd", "RotateCmd" }),
            }, 6);

            CheckMsg(msg);

            var newMsg = CheckParsedVersion<DeviceListVersion0>(msg, 0,
                "[{\"DeviceList\":{\"Devices\":[{\"DeviceName\":\"testDev0\",\"DeviceIndex\":2,\"DeviceMessages\":[\"StopDeviceCmd\",\"VibrateCmd\"]},{\"DeviceName\":\"testDev1\",\"DeviceIndex\":5,\"DeviceMessages\":[\"StopDeviceCmd\",\"RotateCmd\"]}],\"Id\":6}}]"
            );

            CheckMsg(newMsg);
        }


        [Test]
        public void TestVibrateCmd()
        {
            void CheckMsg(VibrateCmd msg)
            {
                msg.Id.Should().Be(4);
                msg.DeviceIndex.Should().Be(2);
                msg.Speeds.Count.Should().Be(1);
                msg.Speeds[0].Index.Should().Be(0);
                msg.Speeds[0].Speed.Should().Be(0.5);
            }

            var msg = new VibrateCmd(2, new List<VibrateCmd.VibrateSubcommand> { new VibrateCmd.VibrateSubcommand(0, 0.5) }, 4);
            CheckMsg(msg);

            var newMsg = CheckParsedVersion<VibrateCmd>(msg, 1,
                "[{\"VibrateCmd\":{\"Speeds\":[{\"Index\":0,\"Speed\":0.5}],\"DeviceIndex\":2,\"Id\":4}}]");

            CheckMsg(newMsg);

            _parser.Invoking(x => x.Serialize(msg, 0)).Should().Throw<ButtplugMessageException>();
        }

        [Test]
        public void TestLinearCmd()
        {
            void CheckMsg(LinearCmd msg)
            {
                msg.Id.Should().Be(4);
                msg.DeviceIndex.Should().Be(2);
                msg.Vectors.Count.Should().Be(1);
                msg.Vectors[0].Index.Should().Be(0);
                msg.Vectors[0].Duration.Should().Be(100);
                msg.Vectors[0].Position.Should().Be(0.5);
            }

            var msg = new LinearCmd(2, new List<LinearCmd.VectorSubcommand> { new LinearCmd.VectorSubcommand(0, 100, 0.5) }, 4);
            CheckMsg(msg);

            var newMsg = CheckParsedVersion<LinearCmd>(msg, 1,
                "[{\"LinearCmd\":{\"Vectors\":[{\"Duration\":100,\"Index\":0,\"Position\":0.5}],\"DeviceIndex\":2,\"Id\":4}}]");

            CheckMsg(newMsg);

            _parser.Invoking(x => x.Serialize(msg, 0)).Should().Throw<ButtplugMessageException>();
        }

        [Test]
        public void TestRotateCmd()
        {
            void CheckMsg(RotateCmd msg)
            {
                msg.Id.Should().Be(4);
                msg.DeviceIndex.Should().Be(2);
                msg.Rotations.Count.Should().Be(1);
                msg.Rotations[0].Index.Should().Be(0);
                msg.Rotations[0].Speed.Should().Be(0.5);
                msg.Rotations[0].Clockwise.Should().Be(true);
            }

            var msg = new RotateCmd(2, new List<RotateCmd.RotateSubcommand> { new RotateCmd.RotateSubcommand(0, 0.5, true) }, 4);
            CheckMsg(msg);

            var newMsg = CheckParsedVersion<RotateCmd>(msg, 1,
                "[{\"RotateCmd\":{\"Rotations\":[{\"Clockwise\":true,\"Index\":0,\"Speed\":0.5}],\"DeviceIndex\":2,\"Id\":4}}]");

            CheckMsg(newMsg);

            _parser.Invoking(x => x.Serialize(msg, 0)).Should().Throw<ButtplugMessageException>();
        }

        [Test]
        public void TestVorzeA10CycloneCmd()
        {
            void CheckMsg(VorzeA10CycloneCmd msg)
            {
                msg.Id.Should().Be(4);
                msg.DeviceIndex.Should().Be(2);
                msg.Speed.Should().Be(50);
                msg.Clockwise.Should().Be(true);
            }

            var msg = new VorzeA10CycloneCmd(2, 50, true, 4);
            CheckMsg(msg);
            var newMsg = CheckParsedVersion<VorzeA10CycloneCmd>(msg, 1,
                "[{\"VorzeA10CycloneCmd\":{\"Clockwise\":true,\"Speed\":50,\"DeviceIndex\":2,\"Id\":4}}]");
            CheckMsg(newMsg);
            newMsg = CheckParsedVersion<VorzeA10CycloneCmd>(msg, 0,
                "[{\"VorzeA10CycloneCmd\":{\"Clockwise\":true,\"Speed\":50,\"DeviceIndex\":2,\"Id\":4}}]");
            CheckMsg(newMsg);

            msg.Invoking(msg => msg.Speed = 1000).Should().Throw<ArgumentException>();
        }

        [Test]
        public void TestRequestLog()
        {
            const string requestLogMsgStr = "[{\"RequestLog\":{\"LogLevel\":\"Debug\",\"Id\":1}}]";
            var requestLogMsg = new RequestLog(ButtplugLogLevel.Debug);
            CheckParsedVersion<RequestLog>(requestLogMsg, 1, requestLogMsgStr);
        }
    }
}
