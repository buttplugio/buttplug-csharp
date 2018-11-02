// <copyright file="LovenseTests.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

// Test file, disable ConfigureAwait checking.
// ReSharper disable ConsiderUsingConfigureAwait

using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Buttplug.Core.Messages;
using Buttplug.Server.Bluetooth.Devices;
using Buttplug.Server.Test.Util;
using JetBrains.Annotations;
using NUnit.Framework;

namespace Buttplug.Server.Test.Bluetooth.Devices
{
    [TestFixture]
    public class LovenseVibratorTests
    {
        [NotNull]
        private BluetoothDeviceTestUtils<LovenseBluetoothInfo> testUtil;

        [SetUp]
        public async Task Init()
        {
            testUtil = new BluetoothDeviceTestUtils<LovenseBluetoothInfo>();

            // Just leave name the same as the prefix, we'll set device type via initialize.
            await testUtil.SetupTest("LVS", false);
            testUtil.AddExpectedRead(testUtil.NoCharacteristic, Encoding.ASCII.GetBytes("W:39:000000000000"));
            await testUtil.Initialize();
        }

        [Test]
        public void TestDeviceName()
        {
            testUtil.TestDeviceName("Lovense Domi v39");
        }

        [Test]
        public void TestAllowedMessages()
        {
            testUtil.TestDeviceAllowedMessages(new Dictionary<System.Type, uint>()
            {
                { typeof(StopDeviceCmd), 0 },
                { typeof(SingleMotorVibrateCmd), 0 },
                { typeof(VibrateCmd), 1 },
                { typeof(LovenseCmd), 0 },
            });
        }

        // StopDeviceCmd noop test handled in GeneralDeviceTests
        [Test]
        public async Task TestStopDeviceCmd()
        {
            var expected =
                new List<(byte[], uint)>()
                {
                    (Encoding.ASCII.GetBytes("Vibrate:10;"), testUtil.NoCharacteristic),
                };

            await testUtil.TestDeviceMessage(new SingleMotorVibrateCmd(4, 0.5), expected, false);

            expected =
                new List<(byte[], uint)>()
                {
                    (Encoding.ASCII.GetBytes("Vibrate:0;"), testUtil.NoCharacteristic),
                };

            await testUtil.TestDeviceMessage(new StopDeviceCmd(4), expected, false);
        }

        [Test]
        public async Task TestSingleMotorVibrateCmd()
        {
            var expected =
                new List<(byte[], uint)>()
                {
                    (Encoding.ASCII.GetBytes("Vibrate:10;"), testUtil.NoCharacteristic),
                };

            await testUtil.TestDeviceMessage(new SingleMotorVibrateCmd(4, 0.5), expected, false);
        }

        [Test]
        public async Task TestVibrateCmd()
        {
            var expected =
                new List<(byte[], uint)>()
                {
                    (Encoding.ASCII.GetBytes("Vibrate:10;"), testUtil.NoCharacteristic),
                };

            await testUtil.TestDeviceMessage(VibrateCmd.Create(4, 1, 0.5, 1), expected, false);
        }

        [Test]
        public void TestInvalidVibrateCmd()
        {
            testUtil.TestInvalidVibrateCmd(1);
        }
    }

    [TestFixture]
    public class LovenseDualVibratorTests
    {
        [NotNull]
        private BluetoothDeviceTestUtils<LovenseBluetoothInfo> testUtil;

        [SetUp]
        public async Task Init()
        {
            testUtil = new BluetoothDeviceTestUtils<LovenseBluetoothInfo>();

            // Just leave name the same as the prefix, we'll set device type via initialize.
            await testUtil.SetupTest("LVS", false);
            testUtil.AddExpectedRead(testUtil.NoCharacteristic, Encoding.ASCII.GetBytes("P:39:000000000000"));
            await testUtil.Initialize();
        }

        [Test]
        public void TestDeviceName()
        {
            testUtil.TestDeviceName("Lovense Edge v39");
        }

        [Test]
        public void TestAllowedMessages()
        {
            testUtil.TestDeviceAllowedMessages(new Dictionary<System.Type, uint>()
            {
                { typeof(StopDeviceCmd), 0 },
                { typeof(SingleMotorVibrateCmd), 0 },
                { typeof(VibrateCmd), 2 },
                { typeof(LovenseCmd), 0 },
            });
        }

        // StopDeviceCmd noop test handled in GeneralDeviceTests
        [Test]
        public async Task TestStopDeviceCmd()
        {
            var expected =
                new List<(byte[], uint)>()
                {
                    (Encoding.ASCII.GetBytes("Vibrate1:10;"), testUtil.NoCharacteristic),
                    (Encoding.ASCII.GetBytes("Vibrate2:10;"), testUtil.NoCharacteristic),
                };

            await testUtil.TestDeviceMessage(new SingleMotorVibrateCmd(4, 0.5), expected, false);

            expected =
                new List<(byte[], uint)>()
                {
                    (Encoding.ASCII.GetBytes("Vibrate1:0;"), testUtil.NoCharacteristic),
                    (Encoding.ASCII.GetBytes("Vibrate2:0;"), testUtil.NoCharacteristic),
                };

            await testUtil.TestDeviceMessage(new StopDeviceCmd(4), expected, false);
        }

        [Test]
        public async Task TestSingleMotorVibrateCmd()
        {
            var expected =
                new List<(byte[], uint)>()
                {
                    (Encoding.ASCII.GetBytes("Vibrate1:10;"), testUtil.NoCharacteristic),
                    (Encoding.ASCII.GetBytes("Vibrate2:10;"), testUtil.NoCharacteristic),
                };

            await testUtil.TestDeviceMessage(new SingleMotorVibrateCmd(4, 0.5), expected, false);
        }

        [Test]
        public async Task TestVibrateCmd()
        {
            var expected =
                new List<(byte[], uint)>()
                {
                    (Encoding.ASCII.GetBytes("Vibrate1:10;"), testUtil.NoCharacteristic),
                    (Encoding.ASCII.GetBytes("Vibrate2:10;"), testUtil.NoCharacteristic),
                };

            await testUtil.TestDeviceMessage(VibrateCmd.Create(4, 1, 0.5, 2), expected, false);
        }

        [Test]
        public void TestInvalidVibrateCmd()
        {
            testUtil.TestInvalidVibrateCmd(2);
        }
    }

    [TestFixture]
    public class LovenseRotatorTests
    {
        [NotNull]
        private BluetoothDeviceTestUtils<LovenseBluetoothInfo> testUtil;

        [SetUp]
        public async Task Init()
        {
            testUtil = new BluetoothDeviceTestUtils<LovenseBluetoothInfo>();

            // Just leave name the same as the prefix, we'll set device type via initialize.
            await testUtil.SetupTest("LVS", false);
            testUtil.AddExpectedRead(testUtil.NoCharacteristic, Encoding.ASCII.GetBytes("A:13:000000000000"));
            await testUtil.Initialize();
        }

        [Test]
        public void TestDeviceName()
        {
            testUtil.TestDeviceName("Lovense Nora v13");
        }

        [Test]
        public void TestAllowedMessages()
        {
            testUtil.TestDeviceAllowedMessages(new Dictionary<System.Type, uint>()
            {
                { typeof(StopDeviceCmd), 0 },
                { typeof(SingleMotorVibrateCmd), 0 },
                { typeof(VibrateCmd), 1 },
                { typeof(RotateCmd), 1 },
                { typeof(LovenseCmd), 0 },
            });
        }

        // StopDeviceCmd noop test handled in GeneralDeviceTests
        [Test]
        public async Task TestStopDeviceCmd()
        {
            var expected =
                new List<(byte[], uint)>()
                {
                    (Encoding.ASCII.GetBytes("Rotate:10;"), testUtil.NoCharacteristic),
                };

            await testUtil.TestDeviceMessage(RotateCmd.Create(4, 1, 0.5, true, 1), expected, false);

            expected =
                new List<(byte[], uint)>()
                {
                    (Encoding.ASCII.GetBytes("Rotate:0;"), testUtil.NoCharacteristic),
                };

            await testUtil.TestDeviceMessage(new StopDeviceCmd(4), expected, false);
        }

        [Test]
        public async Task TestRotateCmd()
        {
            var expected =
                new List<(byte[], uint)>()
                {
                    (Encoding.ASCII.GetBytes("Rotate:10;"), testUtil.NoCharacteristic),
                };

            await testUtil.TestDeviceMessage(RotateCmd.Create(4, 1, 0.5, true, 1), expected, false);

            expected =
                new List<(byte[], uint)>()
                {
                    (Encoding.ASCII.GetBytes("RotateChange;"), testUtil.NoCharacteristic),
                };

            await testUtil.TestDeviceMessage(RotateCmd.Create(4, 1, 0.5, false, 1), expected, false);
        }

        [Test]
        public void TestInvalidVibrateCmd()
        {
            testUtil.TestInvalidDeviceMessage(RotateCmd.Create(4, 1, 0.5, true, 0));
            testUtil.TestInvalidDeviceMessage(RotateCmd.Create(4, 1, 0.5, true, 2));
            testUtil.TestInvalidDeviceMessage(
                new VibrateCmd(4, new List<VibrateCmd.VibrateSubcommand>()
                {
                    new VibrateCmd.VibrateSubcommand(0xffffffff, 0.5),
                }));
        }
    }
}
