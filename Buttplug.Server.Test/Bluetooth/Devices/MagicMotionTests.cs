// <copyright file="MagicMotionTests.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Buttplug.Core.Messages;
using Buttplug.Server.Bluetooth.Devices;
using Buttplug.Server.Test.Util;
using JetBrains.Annotations;
using NUnit.Framework;

namespace Buttplug.Server.Test.Bluetooth.Devices
{
    [TestFixture]
    public class MagicMotionTests
    {
        [NotNull]
        private BluetoothDeviceTestUtils<MagicMotionBluetoothInfo> testUtil;

        [SetUp]
        public async Task Init()
        {
            testUtil = new BluetoothDeviceTestUtils<MagicMotionBluetoothInfo>();
            await testUtil.SetupTest("Smart Mini Vibe");
        }

        [Test]
        public void TestAllowedMessages()
        {
            testUtil.TestDeviceAllowedMessages(new Dictionary<System.Type, uint>()
            {
                { typeof(StopDeviceCmd), 0 },
                { typeof(SingleMotorVibrateCmd), 0 },
                { typeof(VibrateCmd), 1 },
            });
        }

        // StopDeviceCmd noop test handled in GeneralDeviceTests

        [Test]
        public async Task TestStopDeviceCmd()
        {
            var expected =
                new List<(byte[], uint)>()
                {
                    (new byte[] { 0x0b, 0xff, 0x04, 0x0a, 0x32, 0x32, 0x00, 0x04, 0x08, 0x80, 0x64, 0x00 }, (uint)MagicMotionBluetoothInfo.Chrs.Tx),
                };

            await testUtil.TestDeviceMessage(new SingleMotorVibrateCmd(4, 0.5), expected, false);

            expected =
                new List<(byte[], uint)>()
                {
                    (new byte[] { 0x0b, 0xff, 0x04, 0x0a, 0x32, 0x32, 0x00, 0x04, 0x08, 0x00, 0x64, 0x00 }, (uint)MagicMotionBluetoothInfo.Chrs.Tx),
                };

            await testUtil.TestDeviceMessage(new StopDeviceCmd(4), expected, false);
        }

        [Test]
        public async Task TestSingleMotorVibrateCmd()
        {
            var expected =
                new List<(byte[], uint)>()
                {
                    (new byte[] { 0x0b, 0xff, 0x04, 0x0a, 0x32, 0x32, 0x00, 0x04, 0x08, 0x80, 0x64, 0x00 }, (uint)MagicMotionBluetoothInfo.Chrs.Tx),
                };

            await testUtil.TestDeviceMessage(new SingleMotorVibrateCmd(4, 0.5), expected, false);
        }

        [Test]
        public async Task TestVibrateCmd()
        {
            var expected =
                new List<(byte[], uint)>()
                {
                    (new byte[] { 0x0b, 0xff, 0x04, 0x0a, 0x32, 0x32, 0x00, 0x04, 0x08, 0x80, 0x64, 0x00 }, (uint)MagicMotionBluetoothInfo.Chrs.Tx),
                };

            await testUtil.TestDeviceMessage(VibrateCmd.Create(4, 1, 0.5, 1), expected, false);
        }

        [Test]
        public async Task TestInvalidVibrateCmd()
        {
            await testUtil.TestInvalidVibrateCmd(1);
        }
    }
}
