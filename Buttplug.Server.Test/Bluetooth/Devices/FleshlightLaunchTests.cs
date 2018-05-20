using System.Collections.Generic;
using Buttplug.Core.Messages;
using Buttplug.Server.Bluetooth.Devices;
using Buttplug.Server.Test.Util;
using JetBrains.Annotations;
using NUnit.Framework;

namespace Buttplug.Server.Test.Bluetooth.Devices
{
    [TestFixture]
    public class FleshlightLaunchTests
    {
        [NotNull]
        private BluetoothDeviceTestUtils<FleshlightLaunchBluetoothInfo> testUtil;

        [SetUp]
        public void Init()
        {
            testUtil = new BluetoothDeviceTestUtils<FleshlightLaunchBluetoothInfo>();
            testUtil.SetupTest("Launch");
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
        public void TestInitialize()
        {
            testUtil.TestDeviceInitialize(new List<(byte[], uint)>()
            {
                (new byte[1] { 0x0 }, (uint)FleshlightLaunchBluetoothInfo.Chrs.Tx),
            }, true);
        }

        // StopDeviceCmd test handled in GeneralDeviceTests

        // In all device message tests, expect WriteWithResponse to be false.
        [Test]
        public void TestFleshlightLaunchFW12Cmd()
        {
            testUtil.TestDeviceMessage(new FleshlightLaunchFW12Cmd(4, 50, 50),
                new List<(byte[], uint)>()
                {
                    (new byte[2] { 50, 50 }, (uint)FleshlightLaunchBluetoothInfo.Chrs.Tx),
                }, false);
        }

        // TODO Test currently fails because we will send repeated packets to the launch. See #402.
        /*
        [Test]
        public void TestRepeatedFleshlightLaunchFW12Cmd()
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
        public void TestVectorCmd()
        {
            var msg = new LinearCmd(4, new List<LinearCmd.VectorSubcommand>
            {
                new LinearCmd.VectorSubcommand(0, 500, 0.5),
            });
            testUtil.TestDeviceMessage(msg,
                new List<(byte[], uint)>()
                {
                    (new byte[2] { 50, 20 }, (uint)FleshlightLaunchBluetoothInfo.Chrs.Tx),
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