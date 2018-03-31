using System.Collections.Generic;
using Buttplug.Core.Messages;
using Buttplug.Server.Bluetooth.Devices;
using Buttplug.Server.Test.Util;
using JetBrains.Annotations;
using NUnit.Framework;

// TODO Clean up/simplify how we handle per-device testing here.
namespace Buttplug.Server.Test.Bluetooth.Devices
{
    [TestFixture]
    public class VorzeSATests
    {
        private string[] _deviceNames = { "CycSA", "UFOSA" };

        [NotNull]
        private BluetoothDeviceTestUtils<VorzeSABluetoothInfo> testUtil;

        [SetUp]
        public void Init()
        {
        }

        [Test]
        public void TestAllowedMessages()
        {
            foreach (var name in _deviceNames)
            {
                testUtil = new BluetoothDeviceTestUtils<VorzeSABluetoothInfo>();
                testUtil.SetupTest(name);
                testUtil.TestDeviceAllowedMessages(new Dictionary<System.Type, uint>()
                {
                    { typeof(StopDeviceCmd), 0 },
                    { typeof(VorzeA10CycloneCmd), 0 },
                    { typeof(RotateCmd), 1 },
                });
            }
        }

        // StopDeviceCmd noop test handled in GeneralDeviceTests

        [Test]
        public void TestStopDeviceCmd()
        {
            byte deviceIndex = 1;
            foreach (var name in _deviceNames)
            {
                testUtil = new BluetoothDeviceTestUtils<VorzeSABluetoothInfo>();
                testUtil.SetupTest(name);
                var expected = new byte[] { deviceIndex, 0x1, 50 };

                testUtil.TestDeviceMessage(new VorzeA10CycloneCmd(4, 50, false),
                    new List<(byte[], uint)>()
                    {
                        (expected, (uint)VorzeSABluetoothInfo.Chrs.Tx)
                    }, false);

                expected = new byte[] { deviceIndex, 0x1, 0 };

                testUtil.TestDeviceMessage(new StopDeviceCmd(4),
                    new List<(byte[], uint)>()
                    {
                        (expected, (uint)VorzeSABluetoothInfo.Chrs.Tx),
                    }, false);

                ++deviceIndex;
            }
        }

        [Test]
        public void TestVorzeA10CycloneCmd()
        {
            byte deviceIndex = 1;
            foreach (var name in _deviceNames)
            {
                testUtil = new BluetoothDeviceTestUtils<VorzeSABluetoothInfo>();
                testUtil.SetupTest(name);
                var expected = new byte[] { deviceIndex, 0x1, 50 };

                testUtil.TestDeviceMessage(new VorzeA10CycloneCmd(4, 50, false),
                    new List<(byte[], uint)>()
                    {
                        (expected, (uint)VorzeSABluetoothInfo.Chrs.Tx),
                    }, false);

                expected = new byte[] { deviceIndex, 0x1, 50 + 128 };

                testUtil.TestDeviceMessage(new VorzeA10CycloneCmd(4, 50, true),
                    new List<(byte[], uint)>()
                    {
                    (expected, (uint)VorzeSABluetoothInfo.Chrs.Tx),
                    }, false);

                deviceIndex++;
            }
        }

        [Test]
        public void TestRotateCmd()
        {
            byte deviceIndex = 1;
            foreach (var name in _deviceNames)
            {
                testUtil = new BluetoothDeviceTestUtils<VorzeSABluetoothInfo>();
                testUtil.SetupTest(name);
                var expected = new byte[] { deviceIndex, 0x1, 50 };

                testUtil.TestDeviceMessage(
                    RotateCmd.Create(4, 1, 0.5, false, 1),
                    new List<(byte[], uint)>()
                    {
                        (expected, (uint)VorzeSABluetoothInfo.Chrs.Tx),
                    }, false);

                expected = new byte[] { deviceIndex, 0x1, 50 + 128 };

                testUtil.TestDeviceMessage(
                    RotateCmd.Create(4, 1, 0.5, true, 1),
                    new List<(byte[], uint)>()
                    {
                        (expected, (uint)VorzeSABluetoothInfo.Chrs.Tx),
                    }, false);
                deviceIndex++;
            }
        }

        [Test]
        public void TestInvalidVibrateCmd()
        {
            foreach (var name in _deviceNames)
            {
                testUtil = new BluetoothDeviceTestUtils<VorzeSABluetoothInfo>();
                testUtil.SetupTest(name);
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
}