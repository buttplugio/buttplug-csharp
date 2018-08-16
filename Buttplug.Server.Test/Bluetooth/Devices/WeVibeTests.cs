// <copyright file="WeVibeTests.cs" company="Nonpolynomial Labs LLC">
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
    public class WeVibeSingleVibeTests
    {
        [NotNull]
        private BluetoothDeviceTestUtils<WeVibeBluetoothInfo> testUtil;

        [SetUp]
        public async Task Init()
        {
            testUtil = new BluetoothDeviceTestUtils<WeVibeBluetoothInfo>();
            await testUtil.SetupTest("Ditto");
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
                    (new byte[] { 0x0f, 0x03, 0x00, 0x88, 0x00, 0x03, 0x00, 0x00 }, (uint)WeVibeBluetoothInfo.Chrs.Tx),
                };

            await testUtil.TestDeviceMessage(new SingleMotorVibrateCmd(4, 0.5), expected, false);

            expected =
                new List<(byte[], uint)>()
                {
                    (new byte[] { 0x0f, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, (uint)WeVibeBluetoothInfo.Chrs.Tx),
                };

            await testUtil.TestDeviceMessage(new StopDeviceCmd(4), expected, false);
        }

        [Test]
        public async Task TestSingleMotorVibrateCmd()
        {
            var expected =
                new List<(byte[], uint)>()
                {
                    (new byte[] { 0x0f, 0x03, 0x00, 0x88, 0x00, 0x03, 0x00, 0x00 }, (uint)WeVibeBluetoothInfo.Chrs.Tx),
                };

            await testUtil.TestDeviceMessage(new SingleMotorVibrateCmd(4, 0.5), expected, false);
        }

        [Test]
        public async Task TestVibrateCmd()
        {
            var expected =
                new List<(byte[], uint)>()
                {
                    (new byte[] { 0x0f, 0x03, 0x00, 0x88, 0x00, 0x03, 0x00, 0x00 }, (uint)WeVibeBluetoothInfo.Chrs.Tx),
                };

            await testUtil.TestDeviceMessage(VibrateCmd.Create(4, 1, 0.5, 1), expected, false);
        }

        [Test]
        public async Task TestInvalidVibrateCmd()
        {
            await testUtil.TestInvalidVibrateCmd(1);
        }
    }

    [TestFixture]
    public class WeVibeDualVibeTests
    {
        [NotNull]
        private BluetoothDeviceTestUtils<WeVibeBluetoothInfo> testUtil;

        [SetUp]
        public async Task Init()
        {
            testUtil = new BluetoothDeviceTestUtils<WeVibeBluetoothInfo>();
            await testUtil.SetupTest("4plus");
        }

        [Test]
        public void TestAllowedMessages()
        {
            testUtil.TestDeviceAllowedMessages(new Dictionary<System.Type, uint>()
            {
                { typeof(StopDeviceCmd), 0 },
                { typeof(SingleMotorVibrateCmd), 0 },
                { typeof(VibrateCmd), 2 },
            });
        }

        // StopDeviceCmd noop test handled in GeneralDeviceTests

        [Test]
        public async Task TestStopDeviceCmd()
        {
            var expected =
                new List<(byte[], uint)>()
                {
                    (new byte[] { 0x0f, 0x03, 0x00, 0x88, 0x00, 0x03, 0x00, 0x00 }, (uint)WeVibeBluetoothInfo.Chrs.Tx),
                };

            await testUtil.TestDeviceMessage(new SingleMotorVibrateCmd(4, 0.5), expected, false);

            expected =
                new List<(byte[], uint)>()
                {
                    (new byte[] { 0x0f, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, (uint)WeVibeBluetoothInfo.Chrs.Tx),
                };

            await testUtil.TestDeviceMessage(new StopDeviceCmd(4), expected, false);
        }

        [Test]
        public async Task TestSingleMotorVibrateCmd()
        {
            var expected =
                new List<(byte[], uint)>()
                {
                    (new byte[] { 0x0f, 0x03, 0x00, 0x88, 0x00, 0x03, 0x00, 0x00 }, (uint)WeVibeBluetoothInfo.Chrs.Tx),
                };

            await testUtil.TestDeviceMessage(new SingleMotorVibrateCmd(4, 0.5), expected, false);
        }

        [Test]
        public async Task TestVibrateCmd()
        {
            var expected =
                new List<(byte[], uint)>()
                {
                    (new byte[] { 0x0f, 0x03, 0x00, 0x4B, 0x00, 0x03, 0x00, 0x00 }, (uint)WeVibeBluetoothInfo.Chrs.Tx),
                };

            await testUtil.TestDeviceMessage(VibrateCmd.Create(4, 1, new[] { 0.25, 0.75 }, 2), expected, false);
        }

        [Test]
        public async Task TestInvalidCmds()
        {
            await testUtil.TestInvalidVibrateCmd(2);
        }
    }
}
