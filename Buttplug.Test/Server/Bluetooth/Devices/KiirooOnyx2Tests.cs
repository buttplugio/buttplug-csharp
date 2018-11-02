// <copyright file="KiirooOnyx2Tests.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

// Test file, disable ConfigureAwait checking.
// ReSharper disable ConsiderUsingConfigureAwait

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Buttplug.Core.Messages;
using Buttplug.Server.Bluetooth.Devices;
using Buttplug.Server.Test.Util;
using JetBrains.Annotations;
using NUnit.Framework;

namespace Buttplug.Server.Test.Bluetooth.Devices
{
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Test classes can skip documentation requirements")]
    [TestFixture]
    public class KiirooOnyx2Tests
    {
        [NotNull]
        private BluetoothDeviceTestUtils<KiirooOnyx2BluetoothInfo> testUtil;

        [SetUp]
        public async Task Init()
        {
            testUtil = new BluetoothDeviceTestUtils<KiirooOnyx2BluetoothInfo>();
            await testUtil.SetupTest("Onyx2");
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
                (new byte[] { 0x0 }, (uint)KiirooOnyx2BluetoothInfo.Chrs.Cmd),
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
                    (new byte[] { 50, 50 }, (uint)KiirooOnyx2BluetoothInfo.Chrs.Tx),
                }, false);
        }

        // TODO Test currently fails because we will send repeated packets to the launch. See #402.
        /*
        [Test]
        public async Task TestRepeatedFleshlightLaunchFW12Cmd()
        {
            testUtil.TestDeviceMessage(new FleshlightLaunchFW12Cmd(4, 50, 50),
                new List<byte[]>()
                {
                    new byte[2] { 50, 50 },
                }, (uint)FleshlightLaunchBluetoothInfo.Chrs.Tx, false);
            testUtil.TestDeviceMessageNoop(new FleshlightLaunchFW12Cmd(4, 50, 50));
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
                    (new byte[] { 50, 20 }, (uint)KiirooOnyx2BluetoothInfo.Chrs.Tx),
                }, false);
        }

        [Test]
        public void TestInvalidVectorCmdTooManyFeatures()
        {
            var msg = LinearCmd.Create(4, 0, 500, 0.75, 2);
            testUtil.TestInvalidDeviceMessage(msg);
        }

        [Test]
        public void TestInvalidVectorCmdWrongFeatures()
        {
            var msg = new LinearCmd(4,
                new List<LinearCmd.VectorSubcommand>
                {
                    new LinearCmd.VectorSubcommand(0xffffffff, 500, 0.75),
                });
            testUtil.TestInvalidDeviceMessage(msg);
        }

        [Test]
        public void TestInvalidVectorNotEnoughFeatures()
        {
            var msg = LinearCmd.Create(4, 0, 500, 0.75, 0);
            testUtil.TestInvalidDeviceMessage(msg);
        }
    }
}