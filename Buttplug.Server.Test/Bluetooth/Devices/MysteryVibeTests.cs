using System;
using System.Collections.Generic;
using System.Linq;
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
    public class MysteryVibeTests
    {
        [NotNull] private BluetoothDeviceTestUtils<MysteryVibeBluetoothInfo> testUtil;

        [SetUp]
        public void Init()
        {
            testUtil = new BluetoothDeviceTestUtils<MysteryVibeBluetoothInfo>();
            testUtil.SetupTest("MV Crescendo");
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
        public void TestStopDeviceCmd()
        {
            var expected =
                new List<(byte[], uint)>()
                {
                    (Enumerable.Repeat((byte)(MysteryVibe.MaxSpeed * 0.5), 6).ToArray(), testUtil.NoCharacteristic),
                };

            testUtil.TestDeviceMessage(new SingleMotorVibrateCmd(4, 0.5), expected, false);

            expected =
                new List<(byte[], uint)>()
                {
                    (MysteryVibe.NullSpeed, testUtil.NoCharacteristic),
                };

            testUtil.TestDeviceMessageOnWrite(new StopDeviceCmd(4), expected, false);
        }

        [Test]
        public void TestSingleMotorVibrateCmd()
        {
            var expected =
                new List<(byte[], uint)>()
                {
                    (Enumerable.Repeat((byte)(MysteryVibe.MaxSpeed * 0.5), 6).ToArray(), testUtil.NoCharacteristic),
                };

            testUtil.TestDeviceMessage(new SingleMotorVibrateCmd(4, 0.5), expected, false);
        }

        [Test]
        public void TestVibrateCmd()
        {
            var expected =
                new List<(byte[], uint)>()
                {
                    (Enumerable.Repeat((byte)(MysteryVibe.MaxSpeed * 0.5), 6).ToArray(), testUtil.NoCharacteristic),
                };

            testUtil.TestDeviceMessage(VibrateCmd.Create(4, 1, 0.5, 6), expected, false);
        }

        [Test]
        public void TestInvalidVibrateCmd()
        {
            testUtil.TestInvalidVibrateCmd(6);
        }
    }
}