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
    public class KiirooOnyx1Tests
    {
        [NotNull]
        private BluetoothDeviceTestUtils<KiirooBluetoothInfo> testUtil;

        [SetUp]
        public void Init()
        {
            testUtil = new BluetoothDeviceTestUtils<KiirooBluetoothInfo>();
            testUtil.SetupTest("ONYX");
        }

        [Test]
        public void TestAllowedMessages()
        {
            testUtil.TestDeviceAllowedMessages(new Dictionary<System.Type, uint>()
            {
                { typeof(StopDeviceCmd), 0 },
                { typeof(FleshlightLaunchFW12Cmd), 0 },
                { typeof(LinearCmd), 1 },
                { typeof(KiirooCmd), 0 },
            });
        }

        [Test]
        public void TestInitialize()
        {
            testUtil.TestDeviceInitialize(new List<(byte[], uint)>()
            {
                (new byte[] { 0x01, 0x00 }, (uint)KiirooBluetoothInfo.Chrs.Cmd),
                (new byte[] { 0x30, 0x2c }, (uint)KiirooBluetoothInfo.Chrs.Tx),
            }, true, false);
        }

        // StopDeviceCmd test handled in GeneralDeviceTests

        // In all device message tests, expect WriteWithResponse to be false.
        [Test]
        public void TestFleshlightLaunchFW12Cmd()
        {
            var msg = new FleshlightLaunchFW12Cmd(4, 35, 23);
            testUtil.TestDeviceMessageDelayed(msg,
                new List<(byte[], uint)>()
                {
                    (Encoding.ASCII.GetBytes("1,\n"), (uint)KiirooBluetoothInfo.Chrs.Tx),
                    (Encoding.ASCII.GetBytes("2,\n"), (uint)KiirooBluetoothInfo.Chrs.Tx),
                    (Encoding.ASCII.GetBytes("3,\n"), (uint)KiirooBluetoothInfo.Chrs.Tx),
                }, false, 400);

            msg = new FleshlightLaunchFW12Cmd(4, 30, 51);
            testUtil.TestDeviceMessageDelayed(msg,
                new List<(byte[], uint)>()
                {
                    (Encoding.ASCII.GetBytes("2,\n"), (uint)KiirooBluetoothInfo.Chrs.Tx),
                }, false, 500);
        }

        [Test]
        public void TestStopDeviceCmd()
        {
            var expected =
                new List<(byte[], uint)>()
                {
                    (Encoding.ASCII.GetBytes("0,\n"), testUtil.NoCharacteristic),
                };

            testUtil.TestDeviceMessageDelayed(new StopDeviceCmd(4), expected, false, 1000);
        }

        [Test]
        public void TestVectorCmd()
        {
            var msg = new LinearCmd(4, new List<LinearCmd.VectorSubcommand>
            {
                new LinearCmd.VectorSubcommand(0, 500, 0.25),
            });
            testUtil.TestDeviceMessageDelayed(msg,
                new List<(byte[], uint)>()
                {
                    (Encoding.ASCII.GetBytes("1,\n"), (uint)KiirooBluetoothInfo.Chrs.Tx),
                    (Encoding.ASCII.GetBytes("2,\n"), (uint)KiirooBluetoothInfo.Chrs.Tx),
                    (Encoding.ASCII.GetBytes("3,\n"), (uint)KiirooBluetoothInfo.Chrs.Tx),
                }, false, 500);

            msg = new LinearCmd(4, new List<LinearCmd.VectorSubcommand>
            {
                new LinearCmd.VectorSubcommand(0, 400, 0.5),
            });
            testUtil.TestDeviceMessageDelayed(msg,
                new List<(byte[], uint)>()
                {
                    (Encoding.ASCII.GetBytes("2,\n"), (uint)KiirooBluetoothInfo.Chrs.Tx),
                }, false, 500);
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

        [Test]
        public void TestKiirooCmd()
        {
            var expected =
                new List<(byte[], uint)>()
                {
                    (Encoding.ASCII.GetBytes("3,\n"), testUtil.NoCharacteristic),
                };

            testUtil.TestDeviceMessage(new KiirooCmd(4, 3), expected, false);
        }
    }
}