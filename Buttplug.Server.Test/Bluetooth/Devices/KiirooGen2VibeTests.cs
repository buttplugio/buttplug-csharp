using System;
using System.Collections.Generic;
using System.Linq;
using Buttplug.Core.Messages;
using Buttplug.Server.Bluetooth.Devices;
using Buttplug.Server.Test.Util;
using NUnit.Framework;

namespace Buttplug.Server.Test.Bluetooth.Devices
{
    // This info class represents multiple device types, so we can't call setup for our test utils
    // here, they need to be generated per-loop.
    [TestFixture]
    public class KiirooGen2VibeTests
    {
        [Test]
        public void TestAllowedMessages()
        {
            foreach (var item in KiirooGen2Vibe.DevInfos)
            {
                var testUtil = new BluetoothDeviceTestUtils<KiirooGen2VibeBluetoothInfo>();
                testUtil.SetupTest(item.Key);
                testUtil.TestDeviceAllowedMessages(new Dictionary<System.Type, uint>()
                {
                    { typeof(StopDeviceCmd), 0 },
                    { typeof(SingleMotorVibrateCmd), 0 },
                    { typeof(VibrateCmd), item.Value.VibeCount },
                });
            }
        }

        // StopDeviceCmd test handled in GeneralDeviceTests

        [Test]
        public void TestSingleMotorVibrateCmd()
        {
            foreach (var item in KiirooGen2Vibe.DevInfos)
            {
                var testUtil = new BluetoothDeviceTestUtils<KiirooGen2VibeBluetoothInfo>();
                testUtil.SetupTest(item.Key);
                var expected = new byte[] { 0, 0, 0 };
                for (var i = 0u; i < item.Value.VibeCount; ++i)
                {
                    Assert.True(item.Value.VibeOrder.Contains(i));
                    expected[Array.IndexOf(item.Value.VibeOrder, i)] = 50;
                }

                testUtil.TestDeviceMessage(new SingleMotorVibrateCmd(4, 0.5),
                    new List<(byte[], uint)>()
                    {
                        (expected, (uint)KiirooGen2VibeBluetoothInfo.Chrs.Tx),
                    }, false);
            }
        }

        [Test]
        public void TestVibrateCmd()
        {
            foreach (var item in KiirooGen2Vibe.DevInfos)
            {
                var testUtil = new BluetoothDeviceTestUtils<KiirooGen2VibeBluetoothInfo>();
                testUtil.SetupTest(item.Key);
                var speeds = new[] { 0.25, 0.5, 0.75 };
                var features = new List<VibrateCmd.VibrateSubcommand>();
                for (var i = 0u; i < item.Value.VibeCount; ++i)
                {
                    features.Add(new VibrateCmd.VibrateSubcommand(i, speeds[i]));
                }

                var expected = new byte[] { 0, 0, 0 };
                for (var i = 0u; i < item.Value.VibeCount; ++i)
                {
                    Assert.True(item.Value.VibeOrder.Contains(i));
                    expected[Array.IndexOf(item.Value.VibeOrder, i)] = (byte)(speeds[i] * 100);
                }

                testUtil.TestDeviceMessage(new VibrateCmd(4, features),
                    new List<(byte[], uint)>()
                    {
                        (expected, (uint)KiirooGen2VibeBluetoothInfo.Chrs.Tx),
                    }, false);
            }
        }

        [Test]
        public void TestInvalidVibrateCmd()
        {
            foreach (var item in KiirooGen2Vibe.DevInfos)
            {
                var testUtil = new BluetoothDeviceTestUtils<KiirooGen2VibeBluetoothInfo>();
                testUtil.SetupTest(item.Key);
                testUtil.TestInvalidVibrateCmd(item.Value.VibeCount);
            }
        }
    }
}