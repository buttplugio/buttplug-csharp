using System.Collections.Generic;
using System.Linq;
using Buttplug.Core;
using Buttplug.Core.Messages;
using Buttplug.Server.Bluetooth.Devices;
using NUnit.Framework;

namespace Buttplug.Server.Test.Bluetooth.Devices
{
    [TestFixture]
    public class OhMiBodFuseTests
    {
        [Test]
        public void OhMiBodFuseTest()
        {
            var bleInfo = new OhMiBodFuseBluetoothInfo();

            foreach (var chr in new[]
            {
                OhMiBodFuseBluetoothInfo.Chrs.Tx,
                OhMiBodFuseBluetoothInfo.Chrs.RxTouch,
                OhMiBodFuseBluetoothInfo.Chrs.RxAccel,
            })
            {
                Assert.True(bleInfo.Characteristics.Length > (uint)chr);
                Assert.NotNull(bleInfo.Characteristics[(uint)chr]);
            }

            Assert.NotNull(bleInfo.Services);
            Assert.True(bleInfo.Services.Any());
            Assert.NotNull(bleInfo.Services[0]);

            Assert.NotNull(bleInfo.Names);

            // Test the Fuse
            var inter = new TestBluetoothDeviceInterface("Fuse", 2);
            var dev = bleInfo.CreateDevice(new ButtplugLogManager(), inter);
            Assert.AreEqual(3, dev.GetAllowedMessageTypes().Count());
            Assert.True(dev.GetAllowedMessageTypes().Contains(typeof(StopDeviceCmd)));
            Assert.NotNull(dev.GetMessageAttrs(typeof(StopDeviceCmd)));
            Assert.Null(dev.GetMessageAttrs(typeof(StopDeviceCmd)).FeatureCount);
            Assert.True(dev.GetAllowedMessageTypes().Contains(typeof(SingleMotorVibrateCmd)));
            Assert.NotNull(dev.GetMessageAttrs(typeof(SingleMotorVibrateCmd)));
            Assert.Null(dev.GetMessageAttrs(typeof(SingleMotorVibrateCmd)).FeatureCount);
            Assert.True(dev.GetAllowedMessageTypes().Contains(typeof(VibrateCmd)));
            Assert.NotNull(dev.GetMessageAttrs(typeof(VibrateCmd)));
            Assert.AreEqual(2, dev.GetMessageAttrs(typeof(VibrateCmd)).FeatureCount);

            Assert.True(dev.ParseMessage(new StopDeviceCmd(4, 4)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(0, inter.LastWriten.Count);

            Assert.True(dev.ParseMessage(new SingleMotorVibrateCmd(4, 0.5, 6)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(1, inter.LastWriten.Count);
            Assert.AreEqual(6, inter.LastWriten[0].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)OhMiBodFuseBluetoothInfo.Chrs.Tx],
                inter.LastWriten[0].Characteristic);
            Assert.AreEqual(new byte[] { 0x32, 0x32, 0x00 }, inter.LastWriten[0].Value);
            Assert.False(inter.LastWriten[0].WriteWithResponse);
            inter.LastWriten.Clear();

            Assert.True(dev.ParseMessage(new SingleMotorVibrateCmd(4, 0.5, 6)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(0, inter.LastWriten.Count);

            Assert.True(dev.ParseMessage(new SingleMotorVibrateCmd(4, 1, 6)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(1, inter.LastWriten.Count);
            Assert.AreEqual(6, inter.LastWriten[0].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)OhMiBodFuseBluetoothInfo.Chrs.Tx],
                inter.LastWriten[0].Characteristic);
            Assert.AreEqual(new byte[] { 0x64, 0x64, 0x00 }, inter.LastWriten[0].Value);
            Assert.False(inter.LastWriten[0].WriteWithResponse);
            inter.LastWriten.Clear();

            Assert.True(dev.ParseMessage(new SingleMotorVibrateCmd(4, 0.25, 6)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(1, inter.LastWriten.Count);
            Assert.AreEqual(6, inter.LastWriten[0].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)OhMiBodFuseBluetoothInfo.Chrs.Tx],
                inter.LastWriten[0].Characteristic);
            Assert.AreEqual(new byte[] { 0x19, 0x19, 0x00 }, inter.LastWriten[0].Value);
            Assert.False(inter.LastWriten[0].WriteWithResponse);
            inter.LastWriten.Clear();

            Assert.True(dev.ParseMessage(new VibrateCmd(4,
                new List<VibrateCmd.VibrateSubcommand>
                {
                    new VibrateCmd.VibrateSubcommand(0, 0.75),
                    new VibrateCmd.VibrateSubcommand(1, 0.25),
                }, 6)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(1, inter.LastWriten.Count);
            Assert.AreEqual(6, inter.LastWriten[0].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)OhMiBodFuseBluetoothInfo.Chrs.Tx],
                inter.LastWriten[0].Characteristic);
            Assert.AreEqual(new byte[] { 0x19, 0x4B, 0x00 }, inter.LastWriten[0].Value);
            Assert.False(inter.LastWriten[0].WriteWithResponse);
            inter.LastWriten.Clear();

            Assert.True(dev.ParseMessage(new VibrateCmd(4,
                new List<VibrateCmd.VibrateSubcommand>
                {
                    new VibrateCmd.VibrateSubcommand(0, 0.75),
                    new VibrateCmd.VibrateSubcommand(1, 0.25),
                }, 6)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(0, inter.LastWriten.Count);

            Assert.True(dev.ParseMessage(new StopDeviceCmd(4, 9)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(1, inter.LastWriten.Count);
            Assert.AreEqual(9, inter.LastWriten[0].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)OhMiBodFuseBluetoothInfo.Chrs.Tx],
                inter.LastWriten[0].Characteristic);
            Assert.AreEqual(new byte[] { 0x00, 0x00, 0x00 }, inter.LastWriten[0].Value);
            Assert.False(inter.LastWriten[0].WriteWithResponse);
            inter.LastWriten.Clear();

            Assert.True(dev.ParseMessage(new VibrateCmd(4,
                new List<VibrateCmd.VibrateSubcommand>
                {
                    new VibrateCmd.VibrateSubcommand(0, 0.75),
                    new VibrateCmd.VibrateSubcommand(1, 0.75),
                    new VibrateCmd.VibrateSubcommand(2, 0.75),
                }, 8)).GetAwaiter().GetResult() is Error);
            Assert.True(dev.ParseMessage(new VibrateCmd(4,
                new List<VibrateCmd.VibrateSubcommand>
                {
                    new VibrateCmd.VibrateSubcommand(2, 0.75),
                }, 8)).GetAwaiter().GetResult() is Error);
        }
    }
}
