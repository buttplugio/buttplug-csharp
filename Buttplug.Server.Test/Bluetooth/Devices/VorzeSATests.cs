// <copyright file="VorzeSATests.cs" company="Nonpolynomial Labs LLC">
//     Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
//     Copyright (c) Nonpolynomial Labs LLC. All rights reserved. Licensed under the BSD 3-Clause
//     license. See LICENSE file in the project root for full license information.
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
    public class VorzeSATests
    {
        [NotNull]
        private BluetoothDeviceTestUtils<VorzeSABluetoothInfo> testUtil;

        public async Task TestAllowedMessages(string aDeviceName)
        {
            testUtil = new BluetoothDeviceTestUtils<VorzeSABluetoothInfo>();
            await testUtil.SetupTest(aDeviceName);
            testUtil.TestDeviceAllowedMessages(new Dictionary<System.Type, uint>()
                {
                    { typeof(StopDeviceCmd), 0 },
                    { typeof(VorzeA10CycloneCmd), 0 },
                    { typeof(RotateCmd), 1 },
                });
        }

        // StopDeviceCmd noop test handled in GeneralDeviceTests

        public async Task TestStopDeviceCmd(string aDeviceName, byte aPrefix)
        {
            testUtil = new BluetoothDeviceTestUtils<VorzeSABluetoothInfo>();
            await testUtil.SetupTest(aDeviceName);
            var expected = new byte[] { aPrefix, 0x1, 50 };

            await testUtil.TestDeviceMessage(new VorzeA10CycloneCmd(4, 50, false),
                new List<(byte[], uint)>()
                {
                        (expected, (uint)VorzeSABluetoothInfo.Chrs.Tx),
                }, false);

            expected = new byte[] { aPrefix, 0x1, 0 };

            await testUtil.TestDeviceMessage(new StopDeviceCmd(4),
                new List<(byte[], uint)>()
                {
                        (expected, (uint)VorzeSABluetoothInfo.Chrs.Tx),
                }, false);
        }

        public async Task TestVorzeA10CycloneCmd(string aDeviceName, byte aPrefix)
        {
            testUtil = new BluetoothDeviceTestUtils<VorzeSABluetoothInfo>();
            await testUtil.SetupTest(aDeviceName);
            var expected = new byte[] { aPrefix, 0x1, 50 };

            await testUtil.TestDeviceMessage(new VorzeA10CycloneCmd(4, 50, false),
                new List<(byte[], uint)>()
                {
                        (expected, (uint)VorzeSABluetoothInfo.Chrs.Tx),
                }, false);

            expected = new byte[] { aPrefix, 0x1, 50 + 128 };

            await testUtil.TestDeviceMessage(new VorzeA10CycloneCmd(4, 50, true),
                new List<(byte[], uint)>()
                {
                    (expected, (uint)VorzeSABluetoothInfo.Chrs.Tx),
                }, false);
        }

        public async Task TestRotateCmd(string aDeviceName, byte aPrefix)
        {
            testUtil = new BluetoothDeviceTestUtils<VorzeSABluetoothInfo>();
            await testUtil.SetupTest(aDeviceName);
            var expected = new byte[] { aPrefix, 0x1, 50 };

            await testUtil.TestDeviceMessage(
                RotateCmd.Create(4, 1, 0.5, false, 1),
                new List<(byte[], uint)>
                {
                        (expected, (uint)VorzeSABluetoothInfo.Chrs.Tx),
                }, false);

            expected = new byte[] { aPrefix, 0x1, 50 + 128 };

            await testUtil.TestDeviceMessage(
                RotateCmd.Create(4, 1, 0.5, true, 1),
                new List<(byte[], uint)>()
                {
                        (expected, (uint)VorzeSABluetoothInfo.Chrs.Tx),
                }, false);
        }

        public async Task TestInvalidCmds(string aDeviceName)
        {
            testUtil = new BluetoothDeviceTestUtils<VorzeSABluetoothInfo>();
            await testUtil.SetupTest(aDeviceName);
            testUtil.TestInvalidDeviceMessage(RotateCmd.Create(4, 1, 0.5, false, 0));
            testUtil.TestInvalidDeviceMessage(RotateCmd.Create(4, 1, 0.5, false, 2));
            testUtil.TestInvalidDeviceMessage(
                new RotateCmd(4, new List<RotateCmd.RotateSubcommand>()
                {
                        new RotateCmd.RotateSubcommand(0xffffffff, 0.5, true),
                }));
        }
    }
}