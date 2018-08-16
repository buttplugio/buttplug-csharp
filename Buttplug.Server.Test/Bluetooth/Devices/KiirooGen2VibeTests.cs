﻿// <copyright file="KiirooGen2VibeTests.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Buttplug.Core.Messages;
using Buttplug.Server.Bluetooth.Devices;
using Buttplug.Server.Test.Util;
using NUnit.Framework;

namespace Buttplug.Server.Test.Bluetooth.Devices
{
    // This info class represents multiple device types, so we can't call setup for our test utils
    // here, they need to be generated per-loop.
    [TestFixture]
    public class KiirooGen2VibeTests
    {
        [Test]
        public async Task TestAllowedMessages()
        {
            foreach (var item in KiirooGen2Vibe.DevInfos)
            {
                var testUtil = new BluetoothDeviceTestUtils<KiirooGen2VibeBluetoothInfo>();
                await testUtil.SetupTest(item.Key);
                testUtil.TestDeviceAllowedMessages(new Dictionary<System.Type, uint>()
                {
                    { typeof(StopDeviceCmd), 0 },
                    { typeof(SingleMotorVibrateCmd), 0 },
                    { typeof(VibrateCmd), item.Value.VibeCount },
                });
            }
        }

        // StopDeviceCmd test handled in GeneralDeviceTests

        [Test]
        public async Task TestSingleMotorVibrateCmd()
        {
            foreach (var item in KiirooGen2Vibe.DevInfos)
            {
                var testUtil = new BluetoothDeviceTestUtils<KiirooGen2VibeBluetoothInfo>();
                await testUtil.SetupTest(item.Key);
                var expected = new byte[] { 0, 0, 0 };
                for (var i = 0u; i < item.Value.VibeCount; ++i)
                {
                    Assert.True(item.Value.VibeOrder.Contains(i));
                    expected[Array.IndexOf(item.Value.VibeOrder, i)] = 50;
                }

                await testUtil.TestDeviceMessage(new SingleMotorVibrateCmd(4, 0.5),
                    new List<(byte[], uint)>()
                    {
                        (expected, (uint)KiirooGen2VibeBluetoothInfo.Chrs.Tx),
                    }, false);
            }
        }

        [Test]
        public async Task TestVibrateCmd()
        {
            foreach (var item in KiirooGen2Vibe.DevInfos)
            {
                var testUtil = new BluetoothDeviceTestUtils<KiirooGen2VibeBluetoothInfo>();
                await testUtil.SetupTest(item.Key);
                var speeds = new[] { 0.25, 0.5, 0.75 };
                var features = new List<VibrateCmd.VibrateSubcommand>();
                for (var i = 0u; i < item.Value.VibeCount; ++i)
                {
                    features.Add(new VibrateCmd.VibrateSubcommand(i, speeds[i]));
                }

                var expected = new byte[] { 0, 0, 0 };
                for (var i = 0u; i < item.Value.VibeCount; ++i)
                {
                    Assert.True(item.Value.VibeOrder.Contains(i));
                    expected[Array.IndexOf(item.Value.VibeOrder, i)] = (byte)(speeds[i] * 100);
                }

                await testUtil.TestDeviceMessage(new VibrateCmd(4, features),
                    new List<(byte[], uint)>()
                    {
                        (expected, (uint)KiirooGen2VibeBluetoothInfo.Chrs.Tx),
                    }, false);
            }
        }

        [Test]
        public async Task TestInvalidCmds()
        {
            foreach (var item in KiirooGen2Vibe.DevInfos)
            {
                var testUtil = new BluetoothDeviceTestUtils<KiirooGen2VibeBluetoothInfo>();
                await testUtil.SetupTest(item.Key);
                await testUtil.TestInvalidVibrateCmd(item.Value.VibeCount);
            }
        }
    }
}