﻿// <copyright file="MysteryVibeTests.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Buttplug.Core.Messages;
using Buttplug.Server.Bluetooth.Devices;
using Buttplug.Server.Test.Util;
using JetBrains.Annotations;
using NUnit.Framework;

namespace Buttplug.Server.Test.Bluetooth.Devices
{
    [TestFixture]
    public class MysteryVibeTests
    {
        [NotNull]
        private BluetoothDeviceTestUtils<MysteryVibeBluetoothInfo> testUtil;

        [SetUp]
        public async Task Init()
        {
            testUtil = new BluetoothDeviceTestUtils<MysteryVibeBluetoothInfo>();
            await testUtil.SetupTest("MV Crescendo");
        }

        [Test]
        public void TestAllowedMessages()
        {
            testUtil.TestDeviceAllowedMessages(new Dictionary<System.Type, uint>()
            {
                { typeof(StopDeviceCmd), 0 },
                { typeof(SingleMotorVibrateCmd), 0 },
                { typeof(VibrateCmd), 6 },
            });
        }

        // StopDeviceCmd noop test handled in GeneralDeviceTests

        [Test]
        public async Task TestStopDeviceCmd()
        {
            var expected =
                new List<(byte[], uint)>()
                {
                    (Enumerable.Repeat((byte)(MysteryVibe.MaxSpeed * 0.5), 6).ToArray(), testUtil.NoCharacteristic),
                };

            await testUtil.TestDeviceMessage(new SingleMotorVibrateCmd(4, 0.5), expected, false);

            expected =
                new List<(byte[], uint)>()
                {
                    (MysteryVibe.NullSpeed, testUtil.NoCharacteristic),
                };

            await testUtil.TestDeviceMessageOnWrite(new StopDeviceCmd(4), expected, false);
        }

        [Test]
        public async Task TestSingleMotorVibrateCmd()
        {
            var expected =
                new List<(byte[], uint)>()
                {
                    (Enumerable.Repeat((byte)(MysteryVibe.MaxSpeed * 0.5), 6).ToArray(), testUtil.NoCharacteristic),
                };

            await testUtil.TestDeviceMessage(new SingleMotorVibrateCmd(4, 0.5), expected, false);
        }

        [Test]
        public async Task TestVibrateCmd()
        {
            var expected =
                new List<(byte[], uint)>()
                {
                    (Enumerable.Repeat((byte)(MysteryVibe.MaxSpeed * 0.5), 6).ToArray(), testUtil.NoCharacteristic),
                };

            await testUtil.TestDeviceMessage(VibrateCmd.Create(4, 1, 0.5, 6), expected, false);
        }

        [Test]
        public async Task TestInvalidVibrateCmd()
        {
            await testUtil.TestInvalidVibrateCmd(6);
        }
    }
}