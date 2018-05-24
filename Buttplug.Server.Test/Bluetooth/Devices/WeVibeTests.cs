using System.Collections.Generic;
using System.Linq;
using System.Text;
using Buttplug.Core;
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
        public void Init()
        {
            testUtil = new BluetoothDeviceTestUtils<WeVibeBluetoothInfo>();
            testUtil.SetupTest("Ditto");
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
        public void TestStopDeviceCmd()
        {
            var expected =
                new List<(byte[], uint)>()
                {
                    (new byte[] { 0x0f, 0x03, 0x00, 0x88, 0x00, 0x03, 0x00, 0x00 }, (uint)WeVibeBluetoothInfo.Chrs.Tx),
                };

            testUtil.TestDeviceMessage(new SingleMotorVibrateCmd(4, 0.5), expected, false);

            expected =
                new List<(byte[], uint)>()
                {
                    (new byte[] { 0x0f, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, (uint)WeVibeBluetoothInfo.Chrs.Tx),
                };

            testUtil.TestDeviceMessage(new StopDeviceCmd(4), expected, false);
        }

        [Test]
        public void TestSingleMotorVibrateCmd()
        {
            var expected =
                new List<(byte[], uint)>()
                {
                    (new byte[] { 0x0f, 0x03, 0x00, 0x88, 0x00, 0x03, 0x00, 0x00 }, (uint)WeVibeBluetoothInfo.Chrs.Tx),
                };

            testUtil.TestDeviceMessage(new SingleMotorVibrateCmd(4, 0.5), expected, false);
        }

        [Test]
        public void TestVibrateCmd()
        {
            var expected =
                new List<(byte[], uint)>()
                {
                    (new byte[] { 0x0f, 0x03, 0x00, 0x88, 0x00, 0x03, 0x00, 0x00 }, (uint)WeVibeBluetoothInfo.Chrs.Tx),
                };

            testUtil.TestDeviceMessage(VibrateCmd.Create(4, 1, 0.5, 1), expected, false);
        }

        [Test]
        public void TestInvalidVibrateCmd()
        {
            testUtil.TestInvalidVibrateCmd(1);
        }
    }

    [TestFixture]
    public class WeVibeDualVibeTests
    {
        [NotNull]
        private BluetoothDeviceTestUtils<WeVibeBluetoothInfo> testUtil;

        [SetUp]
        public void Init()
        {
            testUtil = new BluetoothDeviceTestUtils<WeVibeBluetoothInfo>();
            testUtil.SetupTest("4plus");
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
        public void TestStopDeviceCmd()
        {
            var expected =
                new List<(byte[], uint)>()
                {
                    (new byte[] { 0x0f, 0x03, 0x00, 0x88, 0x00, 0x03, 0x00, 0x00 }, (uint)WeVibeBluetoothInfo.Chrs.Tx),
                };

            testUtil.TestDeviceMessage(new SingleMotorVibrateCmd(4, 0.5), expected, false);

            expected =
                new List<(byte[], uint)>()
                {
                    (new byte[] { 0x0f, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, (uint)WeVibeBluetoothInfo.Chrs.Tx),
                };

            testUtil.TestDeviceMessage(new StopDeviceCmd(4), expected, false);
        }

        [Test]
        public void TestSingleMotorVibrateCmd()
        {
            var expected =
                new List<(byte[], uint)>()
                {
                    (new byte[] { 0x0f, 0x03, 0x00, 0x88, 0x00, 0x03, 0x00, 0x00 }, (uint)WeVibeBluetoothInfo.Chrs.Tx),
                };

            testUtil.TestDeviceMessage(new SingleMotorVibrateCmd(4, 0.5), expected, false);
        }

        [Test]
        public void TestVibrateCmd()
        {
            var expected =
                new List<(byte[], uint)>()
                {
                    (new byte[] { 0x0f, 0x03, 0x00, 0x4B, 0x00, 0x03, 0x00, 0x00 }, (uint)WeVibeBluetoothInfo.Chrs.Tx),
                };

            testUtil.TestDeviceMessage(VibrateCmd.Create(4, 1, new[] { 0.25, 0.75 }, 2), expected, false);
        }

        [Test]
        public void TestInvalidVibrateCmd()
        {
            testUtil.TestInvalidVibrateCmd(2);
        }
    }
}
