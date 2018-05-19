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
    public class VorzeA10CycloneTests
    {
        [NotNull]
        private BluetoothDeviceTestUtils<VorzeA10CycloneBluetoothInfo> testUtil;

        [SetUp]
        public void Init()
        {
            testUtil = new BluetoothDeviceTestUtils<VorzeA10CycloneBluetoothInfo>();
            testUtil.SetupTest("CycSA");
        }

        [Test]
        public void TestAllowedMessages()
        {
            testUtil.TestDeviceAllowedMessages(new Dictionary<System.Type, uint>()
            {
                { typeof(StopDeviceCmd), 0 },
                { typeof(VorzeA10CycloneCmd), 0 },
                { typeof(RotateCmd), 1 },
            });
        }

        // StopDeviceCmd noop test handled in GeneralDeviceTests

        [Test]
        public void TestStopDeviceCmd()
        {
            var expected = new byte[] { 0x1, 0x1, 50 };

            testUtil.TestDeviceMessage(new VorzeA10CycloneCmd(4, 50, false),
                new List<(byte[], uint)>()
                {
                    (expected, (uint)VorzeA10CycloneBluetoothInfo.Chrs.Tx)
                }, false);

            expected = new byte[] { 0x1, 0x1, 0 };

            testUtil.TestDeviceMessage(new StopDeviceCmd(4),
                new List<(byte[], uint)>()
                {
                    (expected, (uint)VorzeA10CycloneBluetoothInfo.Chrs.Tx),
                }, false);
        }

        [Test]
        public void TestVorzeA10CycloneCmd()
        {
            var expected = new byte[] { 0x1, 0x1, 50 };

            testUtil.TestDeviceMessage(new VorzeA10CycloneCmd(4, 50, false),
                new List<(byte[], uint)>()
                {
                        (expected, (uint)VorzeA10CycloneBluetoothInfo.Chrs.Tx),
                }, false);

            expected = new byte[] { 0x1, 0x1, 50 + 128 };

            testUtil.TestDeviceMessage(new VorzeA10CycloneCmd(4, 50, true),
                new List<(byte[], uint)>()
                {
                    (expected, (uint)VorzeA10CycloneBluetoothInfo.Chrs.Tx),
                }, false);
        }

        [Test]
        public void TestRotateCmd()
        {
            var expected = new byte[] { 0x1, 0x1, 50 };

            testUtil.TestDeviceMessage(
                new RotateCmd(4, new List<RotateCmd.RotateSubcommand>()
                {
                    new RotateCmd.RotateSubcommand(0, 0.5, false),
                }),
                new List<(byte[], uint)>()
                {
                    (expected, (uint)VorzeA10CycloneBluetoothInfo.Chrs.Tx),
                }, false);

            expected = new byte[] { 0x1, 0x1, 50 + 128 };

            testUtil.TestDeviceMessage(
                new RotateCmd(4, new List<RotateCmd.RotateSubcommand>()
                {
                    new RotateCmd.RotateSubcommand(0, 0.5, true),
                }),
                new List<(byte[], uint)>()
                {
                    (expected, (uint)VorzeA10CycloneBluetoothInfo.Chrs.Tx),
                }, false);
        }

        [Test]
        public void TestInvalidVibrateCmd()
        {
            testUtil.TestInvalidDeviceMessage(
                new RotateCmd(4, new List<RotateCmd.RotateSubcommand>()
                {
                }));
            testUtil.TestInvalidDeviceMessage(
                new RotateCmd(4, new List<RotateCmd.RotateSubcommand>()
                {
                    new RotateCmd.RotateSubcommand(0, 0.5, true),
                    new RotateCmd.RotateSubcommand(1, 0.5, true),
                }));
            testUtil.TestInvalidDeviceMessage(
                new RotateCmd(4, new List<RotateCmd.RotateSubcommand>()
                {
                    new RotateCmd.RotateSubcommand(0xffffffff, 0.5, true),
                }));
        }
    }
}
