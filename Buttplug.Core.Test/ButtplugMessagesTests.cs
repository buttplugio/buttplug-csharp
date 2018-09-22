// <copyright file="ButtplugMessagesTests.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using FluentAssertions;
using NUnit.Framework;

namespace Buttplug.Core.Test
{
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

        [Test]
        public void TestLovenseCmd()
        {
            var msg = new LovenseCmd(2, "Vibrate:2;", 4);
            Assert.AreEqual(2, msg.DeviceIndex);
            Assert.AreEqual(4, msg.Id);
            Assert.AreEqual("Vibrate:2;", msg.Command);

            var str = _parser.Serialize(msg, 0);
            Assert.AreEqual("[{\"LovenseCmd\":{\"Command\":\"Vibrate:2;\",\"DeviceIndex\":2,\"Id\":4}}]", str);

            var msgs = _parser.Deserialize(str);
            Assert.AreEqual(1, msgs.Length);
            Assert.True(msgs[0] is LovenseCmd);
            msg = (LovenseCmd)msgs[0];
            Assert.AreEqual(2, msg.DeviceIndex);
            Assert.AreEqual(4, msg.Id);
            Assert.AreEqual("Vibrate:2;", msg.Command);
        }

        [Test]
        public void TestDeviceAddedCmd()
        {
            var msg = new DeviceAdded(2, "testDev", new Dictionary<string, MessageAttributes>
            {
                { "StopDeviceCmd", new MessageAttributes() },
                { "VibrateCmd", new MessageAttributes() { FeatureCount = 1 } },
            });

            Assert.AreEqual(2, msg.DeviceIndex);
            Assert.AreEqual(0, msg.Id);
            Assert.AreEqual("testDev", msg.DeviceName);
            Assert.AreEqual(2, msg.DeviceMessages.Count);

            var str1 = _parser.Serialize(msg, 1);
            Assert.AreEqual(
                "[{\"DeviceAdded\":{\"DeviceName\":\"testDev\",\"DeviceMessages\":{\"StopDeviceCmd\":{},\"VibrateCmd\":{\"FeatureCount\":1}},\"DeviceIndex\":2,\"Id\":0}}]",
                str1);

            var msgs = _parser.Deserialize(str1);
            Assert.AreEqual(1, msgs.Length);
            Assert.True(msgs[0] is DeviceAdded);
            var msg1 = (DeviceAdded)msgs[0];
            Assert.AreEqual(2, msg1.DeviceIndex);
            Assert.AreEqual(0, msg1.Id);
            Assert.AreEqual("testDev", msg1.DeviceName);
            Assert.AreEqual(2, msg1.DeviceMessages.Count);
            Assert.Contains("StopDeviceCmd", msg1.DeviceMessages.Keys);
            Assert.Contains("VibrateCmd", msg1.DeviceMessages.Keys);

            var str0 = _parser.Serialize(msg, 0);
            Assert.AreEqual(
                "[{\"DeviceAdded\":{\"DeviceName\":\"testDev\",\"DeviceMessages\":[\"StopDeviceCmd\",\"VibrateCmd\"],\"DeviceIndex\":2,\"Id\":0}}]",
                str0);

            msgs = _parser.Deserialize(str0);
            Assert.AreEqual(1, msgs.Length);
            Assert.True(msgs[0] is DeviceAddedVersion0);
            var msg0 = (DeviceAddedVersion0)msgs[0];
            Assert.AreEqual(2, msg0.DeviceIndex);
            Assert.AreEqual(0, msg0.Id);
            Assert.AreEqual("testDev", msg0.DeviceName);
            Assert.AreEqual(2, msg0.DeviceMessages.Length);
            Assert.Contains("StopDeviceCmd", msg0.DeviceMessages);
            Assert.Contains("VibrateCmd", msg0.DeviceMessages);
        }

        [Test]
        public void TestDeviceListCmd()
        {
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

            Assert.AreEqual(6, msg.Id);
            Assert.AreEqual(2, msg.Devices.Length);
            Assert.AreEqual("testDev0", msg.Devices[0].DeviceName);
            Assert.AreEqual(2, msg.Devices[0].DeviceIndex);
            Assert.AreEqual(2, msg.Devices[0].DeviceMessages.Count);
            Assert.Contains("StopDeviceCmd", msg.Devices[0].DeviceMessages.Keys);
            Assert.Null(msg.Devices[0].DeviceMessages["StopDeviceCmd"].FeatureCount);
            Assert.Contains("VibrateCmd", msg.Devices[0].DeviceMessages.Keys);
            Assert.AreEqual(1, msg.Devices[0].DeviceMessages["VibrateCmd"].FeatureCount);
            Assert.AreEqual("testDev1", msg.Devices[1].DeviceName);
            Assert.AreEqual(5, msg.Devices[1].DeviceIndex);
            Assert.AreEqual(2, msg.Devices[1].DeviceMessages.Count);
            Assert.Contains("StopDeviceCmd", msg.Devices[1].DeviceMessages.Keys);
            Assert.Null(msg.Devices[1].DeviceMessages["StopDeviceCmd"].FeatureCount);
            Assert.Contains("RotateCmd", msg.Devices[1].DeviceMessages.Keys);
            Assert.AreEqual(2, msg.Devices[1].DeviceMessages["RotateCmd"].FeatureCount);

            var str1 = _parser.Serialize(msg, 1);
            Assert.AreEqual(
                "[{\"DeviceList\":{\"Devices\":[{\"DeviceName\":\"testDev0\",\"DeviceIndex\":2,\"DeviceMessages\":{\"StopDeviceCmd\":{},\"VibrateCmd\":{\"FeatureCount\":1}}},{\"DeviceName\":\"testDev1\",\"DeviceIndex\":5,\"DeviceMessages\":{\"StopDeviceCmd\":{},\"RotateCmd\":{\"FeatureCount\":2}}}],\"Id\":6}}]",
                str1);

            var msgs = _parser.Deserialize(str1);
            Assert.AreEqual(1, msgs.Length);
            Assert.True(msgs[0] is DeviceList);
            var msg1 = (DeviceList)msgs[0];
            Assert.AreEqual(6, msg1.Id);
            Assert.AreEqual(2, msg1.Devices.Length);
            Assert.AreEqual("testDev0", msg1.Devices[0].DeviceName);
            Assert.AreEqual(2, msg1.Devices[0].DeviceIndex);
            Assert.AreEqual(2, msg1.Devices[0].DeviceMessages.Count);
            Assert.Contains("StopDeviceCmd", msg1.Devices[0].DeviceMessages.Keys);
            Assert.Null(msg1.Devices[0].DeviceMessages["StopDeviceCmd"].FeatureCount);
            Assert.Contains("VibrateCmd", msg1.Devices[0].DeviceMessages.Keys);
            Assert.AreEqual(1, msg1.Devices[0].DeviceMessages["VibrateCmd"].FeatureCount);
            Assert.AreEqual("testDev1", msg1.Devices[1].DeviceName);
            Assert.AreEqual(5, msg1.Devices[1].DeviceIndex);
            Assert.AreEqual(2, msg1.Devices[1].DeviceMessages.Count);
            Assert.Contains("StopDeviceCmd", msg1.Devices[1].DeviceMessages.Keys);
            Assert.Null(msg1.Devices[1].DeviceMessages["StopDeviceCmd"].FeatureCount);
            Assert.Contains("RotateCmd", msg1.Devices[1].DeviceMessages.Keys);
            Assert.AreEqual(2, msg1.Devices[1].DeviceMessages["RotateCmd"].FeatureCount);

            var str0 = _parser.Serialize(msg, 0);
            Assert.AreEqual(
                "[{\"DeviceList\":{\"Devices\":[{\"DeviceName\":\"testDev0\",\"DeviceIndex\":2,\"DeviceMessages\":[\"StopDeviceCmd\",\"VibrateCmd\"]},{\"DeviceName\":\"testDev1\",\"DeviceIndex\":5,\"DeviceMessages\":[\"StopDeviceCmd\",\"RotateCmd\"]}],\"Id\":6}}]",
                str0);

            msgs = _parser.Deserialize(str0);
            Assert.AreEqual(1, msgs.Length);
            Assert.True(msgs[0] is DeviceListVersion0);
            var msg0 = (DeviceListVersion0)msgs[0];
            Assert.AreEqual(6, msg0.Id);
            Assert.AreEqual(2, msg0.Devices.Length);
            Assert.AreEqual("testDev0", msg0.Devices[0].DeviceName);
            Assert.AreEqual(2, msg0.Devices[0].DeviceIndex);
            Assert.AreEqual(2, msg0.Devices[0].DeviceMessages.Length);
            Assert.Contains("StopDeviceCmd", msg0.Devices[0].DeviceMessages);
            Assert.Contains("VibrateCmd", msg0.Devices[0].DeviceMessages);
            Assert.AreEqual("testDev1", msg0.Devices[1].DeviceName);
            Assert.AreEqual(5, msg0.Devices[1].DeviceIndex);
            Assert.AreEqual(2, msg0.Devices[1].DeviceMessages.Length);
            Assert.Contains("StopDeviceCmd", msg0.Devices[1].DeviceMessages);
            Assert.Contains("RotateCmd", msg0.Devices[1].DeviceMessages);

            msg0 = new DeviceListVersion0(new[]
            {
                new DeviceMessageInfoVersion0(2, "testDev0", new[] { "StopDeviceCmd", "VibrateCmd" }),
                new DeviceMessageInfoVersion0(5, "testDev1", new[] { "StopDeviceCmd", "RotateCmd" }),
            }, 6);
            Assert.AreEqual(6, msg0.Id);
            Assert.AreEqual(2, msg0.Devices.Length);
            Assert.AreEqual("testDev0", msg0.Devices[0].DeviceName);
            Assert.AreEqual(2, msg0.Devices[0].DeviceIndex);
            Assert.AreEqual(2, msg0.Devices[0].DeviceMessages.Length);
            Assert.Contains("StopDeviceCmd", msg0.Devices[0].DeviceMessages);
            Assert.Contains("VibrateCmd", msg0.Devices[0].DeviceMessages);
            Assert.AreEqual("testDev1", msg0.Devices[1].DeviceName);
            Assert.AreEqual(5, msg0.Devices[1].DeviceIndex);
            Assert.AreEqual(2, msg0.Devices[1].DeviceMessages.Length);
            Assert.Contains("StopDeviceCmd", msg0.Devices[1].DeviceMessages);
            Assert.Contains("RotateCmd", msg0.Devices[1].DeviceMessages);
        }

        [Test]
        public void TestRotateCmd()
        {
            var msg = new RotateCmd(2, new List<RotateCmd.RotateSubcommand> { new RotateCmd.RotateSubcommand(0, 0.5, true) }, 4);
            Assert.AreEqual(2, msg.DeviceIndex);
            Assert.AreEqual(4, msg.Id);
            Assert.AreEqual(1, msg.Rotations.Count);
            Assert.AreEqual(0, msg.Rotations[0].Index);
            Assert.AreEqual(0.5, msg.Rotations[0].Speed);
            Assert.True(msg.Rotations[0].Clockwise);

            var str1 = _parser.Serialize(msg, 1);
            Assert.AreEqual(
                "[{\"RotateCmd\":{\"Rotations\":[{\"Index\":0,\"Clockwise\":true,\"Speed\":0.5}],\"DeviceIndex\":2,\"Id\":4}}]", str1);

            var msgs = _parser.Deserialize(str1);
            Assert.AreEqual(1, msgs.Length);
            Assert.True(msgs[0] is RotateCmd);
            msg = (RotateCmd)msgs[0];
            Assert.AreEqual(2, msg.DeviceIndex);
            Assert.AreEqual(4, msg.Id);
            Assert.AreEqual(1, msg.Rotations.Count);
            Assert.AreEqual(0, msg.Rotations[0].Index);
            Assert.AreEqual(0.5, msg.Rotations[0].Speed);
            Assert.True(msg.Rotations[0].Clockwise);

            var str0 = _parser.Serialize(msg, 0);
            Assert.AreEqual("[{\"Error\":{\"ErrorCode\":3,\"ErrorMessage\":\"No backwards compatible version for message #RotateCmd!\",\"Id\":4}}]", str0);
        }

        [Test]
        public void TestVorzeA10CycloneCmd()
        {
            var msg = new VorzeA10CycloneCmd(2, 50, true, 4);
            Assert.AreEqual(2, msg.DeviceIndex);
            Assert.AreEqual(4, msg.Id);
            Assert.AreEqual(50, msg.Speed);
            Assert.True(msg.Clockwise);

            Assert.Catch<ArgumentException>(() =>
            {
                msg.Speed = 1000;
            });

            var str1 = _parser.Serialize(msg, 1);
            Assert.AreEqual(
                "[{\"VorzeA10CycloneCmd\":{\"Clockwise\":true,\"Speed\":50,\"DeviceIndex\":2,\"Id\":4}}]", str1);

            var msgs = _parser.Deserialize(str1);
            Assert.AreEqual(1, msgs.Length);
            Assert.True(msgs[0] is VorzeA10CycloneCmd);
            msg = (VorzeA10CycloneCmd)msgs[0];
            Assert.AreEqual(2, msg.DeviceIndex);
            Assert.AreEqual(4, msg.Id);
            Assert.AreEqual(50, msg.Speed);
            Assert.True(msg.Clockwise);

            var str0 = _parser.Serialize(msg, 0);
            Assert.AreEqual(
                "[{\"VorzeA10CycloneCmd\":{\"Clockwise\":true,\"Speed\":50,\"DeviceIndex\":2,\"Id\":4}}]", str1);

            msgs = _parser.Deserialize(str1);
            Assert.AreEqual(1, msgs.Length);
            Assert.True(msgs[0] is VorzeA10CycloneCmd);
            msg = (VorzeA10CycloneCmd)msgs[0];
            Assert.AreEqual(2, msg.DeviceIndex);
            Assert.AreEqual(4, msg.Id);
            Assert.AreEqual(50, msg.Speed);
            Assert.True(msg.Clockwise);
        }

        [Test]
        public void TestRequestLog()
        {
            const string requestLogMsgStr = "[{\"RequestLog\":{\"LogLevel\":\"Debug\",\"Id\":1}}]";
            var requestLogMsg = new RequestLog(ButtplugLogLevel.Debug);
            var requestLogMsgJson = _parser.Serialize(requestLogMsg, 1);
            Assert.AreEqual(requestLogMsgStr, requestLogMsgJson);

            var requestLogMsgParsed = _parser.Deserialize(requestLogMsgStr);
            requestLogMsg.Should().BeEquivalentTo(requestLogMsgParsed[0]);
        }
    }
}
