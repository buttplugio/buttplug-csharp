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
    public class VibratissimoTests
    {
        [NotNull]
        private BluetoothDeviceTestUtils<VibratissimoBluetoothInfo> testUtil;

        [SetUp]
        public void Init()
        {
            testUtil = new BluetoothDeviceTestUtils<VibratissimoBluetoothInfo>();
            testUtil.SetupTest("Vibratissimo");
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
        public void TestSingleMotorVibrateCmd()
        {
            var expected = new List<(byte[], uint)>()
            {
                (new byte[] { 0x03, 0xff }, (uint)VibratissimoBluetoothInfo.Chrs.TxMode),
                (new byte[] { 0x80, 0x00 }, (uint)VibratissimoBluetoothInfo.Chrs.TxSpeed),
            };

            testUtil.TestDeviceMessage(new SingleMotorVibrateCmd(4, 0.5), expected, false);
            testUtil.TestDeviceMessageNoop(new SingleMotorVibrateCmd(4, 0.5));

            expected = new List<(byte[], uint)>()
            {
                (new byte[] { 0x03, 0xff }, (uint)VibratissimoBluetoothInfo.Chrs.TxMode),
                (new byte[] { 0xff, 0x00 }, (uint)VibratissimoBluetoothInfo.Chrs.TxSpeed),
            };
            testUtil.TestDeviceMessage(new SingleMotorVibrateCmd(4, 1), expected, false);
        }

        [Test]
        public void TestStopDeviceCmd()
        {
            var expected = new List<(byte[], uint)>()
            {
                (new byte[] { 0x03, 0xff }, (uint)VibratissimoBluetoothInfo.Chrs.TxMode),
                (new byte[] { 0x80, 0x00 }, (uint)VibratissimoBluetoothInfo.Chrs.TxSpeed),
            };

            testUtil.TestDeviceMessage(new SingleMotorVibrateCmd(4, 0.5), expected, false);

            expected = new List<(byte[], uint)>()
            {
                (new byte[] { 0x03, 0xff }, (uint)VibratissimoBluetoothInfo.Chrs.TxMode),
                (new byte[] { 0x00, 0x00 }, (uint)VibratissimoBluetoothInfo.Chrs.TxSpeed),
            };
            testUtil.TestDeviceMessage(new StopDeviceCmd(4), expected, false);
        }

        [Test]
        public void TestVibrateCmd()
        {
            var expected = new List<(byte[], uint)>()
            {
                (new byte[] { 0x03, 0xff }, (uint)VibratissimoBluetoothInfo.Chrs.TxMode),
                (new byte[] { 0x80, 0x00 }, (uint)VibratissimoBluetoothInfo.Chrs.TxSpeed),
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
