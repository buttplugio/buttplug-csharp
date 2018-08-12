// <copyright file="KiirooTests.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
using System.Text;
using Buttplug.Core.Messages;
using Buttplug.Server.Bluetooth.Devices;
using Buttplug.Server.Test.Util;
using JetBrains.Annotations;
using NUnit.Framework;

namespace Buttplug.Server.Test.Bluetooth.Devices
{
    [TestFixture]
    public class KiirooVibratorTests
    {
        [NotNull]
        private BluetoothDeviceTestUtils<KiirooBluetoothInfo> testUtil;

        [SetUp]
        public void Init()
        {
            testUtil = new BluetoothDeviceTestUtils<KiirooBluetoothInfo>();
            testUtil.SetupTest("PEARL");
        }

        [Test]
        public void TestAllowedMessages()
        {
            testUtil.TestDeviceAllowedMessages(new Dictionary<System.Type, uint>()
            {
                { typeof(KiirooCmd), 0 },
                { typeof(StopDeviceCmd), 0 },
                { typeof(SingleMotorVibrateCmd), 0 },
                { typeof(VibrateCmd), 1 },
            });
        }

        // StopDeviceCmd noop test handled in GeneralDeviceTests

        [Test]
        public void TestStopDeviceCmd()
        {
            var expected =
                new List<(byte[], uint)>()
                {
                    (Encoding.ASCII.GetBytes("2,\n"), testUtil.NoCharacteristic),
                };

            testUtil.TestDeviceMessage(new SingleMotorVibrateCmd(4, 0.5), expected, false);

            expected =
                new List<(byte[], uint)>()
                {
                    (Encoding.ASCII.GetBytes("0,\n"), testUtil.NoCharacteristic),
                };

            testUtil.TestDeviceMessage(new StopDeviceCmd(4), expected, false);
        }

        [Test]
        public void TestSingleMotorVibrateCmd()
        {
            var expected =
                new List<(byte[], uint)>()
                {
                    (Encoding.ASCII.GetBytes("2,\n"), testUtil.NoCharacteristic),
                };

            testUtil.TestDeviceMessage(new SingleMotorVibrateCmd(4, 0.5), expected, false);
        }

        [Test]
        public void TestVibrateCmd()
        {
            var expected =
                new List<(byte[], uint)>()
                {
                    (Encoding.ASCII.GetBytes("2,\n"), testUtil.NoCharacteristic),
                };

            testUtil.TestDeviceMessage(VibrateCmd.Create(4, 1, 0.5, 1), expected, false);
        }

        [Test]
        public void TestInvalidVibrateCmd()
        {
            testUtil.TestInvalidVibrateCmd(1);
        }
    }
}
