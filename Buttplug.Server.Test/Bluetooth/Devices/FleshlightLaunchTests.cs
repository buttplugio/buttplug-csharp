// <copyright file="FleshlightLaunchTests.cs" company="Nonpolynomial Labs LLC">
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
    public class FleshlightLaunchTests
    {
        [NotNull]
        private BluetoothDeviceTestUtils<FleshlightLaunchBluetoothInfo> testUtil;

        [SetUp]
        public async Task Init()
        {
            testUtil = new BluetoothDeviceTestUtils<FleshlightLaunchBluetoothInfo>();
            await testUtil.SetupTest("Launch");
        }

        [Test]
        public void TestAllowedMessages()
        {
            testUtil.TestDeviceAllowedMessages(new Dictionary<System.Type, uint>()
            {
                { typeof(StopDeviceCmd), 0 },
                { typeof(FleshlightLaunchFW12Cmd), 0 },
                { typeof(LinearCmd), 1 },
            });
        }

        [Test]
        public async Task TestInitialize()
        {
            await testUtil.TestDeviceInitialize(new List<(byte[], uint)>()
            {
                (new byte[1] { 0x0 }, (uint)FleshlightLaunchBluetoothInfo.Chrs.Cmd),
            }, true);
        }

        // StopDeviceCmd test handled in GeneralDeviceTests

        // In all device message tests, expect WriteWithResponse to be false.
        [Test]
        public async Task TestFleshlightLaunchFW12Cmd()
        {
            await testUtil.TestDeviceMessage(new FleshlightLaunchFW12Cmd(4, 50, 50),
                new List<(byte[], uint)>()
                {
                    (new byte[2] { 50, 50 }, (uint)FleshlightLaunchBluetoothInfo.Chrs.Tx),
                }, false);
        }

        // TODO Test currently fails because we will send repeated packets to the launch. See #402.
        /*
        [Test]
        public async Task TestRepeatedFleshlightLaunchFW12Cmd()
        {
            await testUtil.TestDeviceMessage(new FleshlightLaunchFW12Cmd(4, 50, 50),
                new List<byte[]>()
                {
                    new byte[2] { 50, 50 },
                }, (uint)FleshlightLaunchBluetoothInfo.Chrs.Tx, false);
            await testUtil.TestDeviceMessageNoop(new FleshlightLaunchFW12Cmd(4, 50, 50));
        }
        */

        [Test]
        public async Task TestVectorCmd()
        {
            var msg = new LinearCmd(4, new List<LinearCmd.VectorSubcommand>
            {
                new LinearCmd.VectorSubcommand(0, 500, 0.5),
            });
            await testUtil.TestDeviceMessage(msg,
                new List<(byte[], uint)>()
                {
                    (new byte[2] { 50, 20 }, (uint)FleshlightLaunchBluetoothInfo.Chrs.Tx),
                }, false);
        }

        [Test]
        public async Task TestInvalidVectorCmdTooManyFeatures()
        {
            var msg = LinearCmd.Create(4, 0, 500, 0.75, 2);
            await testUtil.TestInvalidDeviceMessage(msg);
        }

        [Test]
        public async Task TestInvalidVectorCmdWrongFeatures()
        {
            var msg = new LinearCmd(4,
                new List<LinearCmd.VectorSubcommand>
                {
                    new LinearCmd.VectorSubcommand(0xffffffff, 500, 0.75),
                });
            await testUtil.TestInvalidDeviceMessage(msg);
        }

        [Test]
        public async Task TestInvalidVectorNotEnoughFeatures()
        {
            var msg = LinearCmd.Create(4, 0, 500, 0.75, 0);
            await testUtil.TestInvalidDeviceMessage(msg);
        }
    }
}