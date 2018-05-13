using System.Collections.Generic;
using System.Linq;
using Buttplug.Core;
using Buttplug.Core.Messages;
using Buttplug.Server.Bluetooth.Devices;
using NUnit.Framework;

namespace Buttplug.Server.Test.Bluetooth.Devices
{
    [TestFixture]
    public class VibratissimoTests
    {
        [Test]
        public void VibratissimoTest()
        {
            var bleInfo = new VibratissimoBluetoothInfo();

            foreach (var chr in new[]
            {
                VibratissimoBluetoothInfo.Chrs.Rx,
                VibratissimoBluetoothInfo.Chrs.TxMode,
                VibratissimoBluetoothInfo.Chrs.TxSpeed,
            })
            {
                Assert.True(bleInfo.Characteristics.Length > (uint)chr);
                Assert.NotNull(bleInfo.Characteristics[(uint)chr]);
            }

            Assert.NotNull(bleInfo.Services);
            Assert.True(bleInfo.Services.Any());
            Assert.NotNull(bleInfo.Services[0]);

            Assert.NotNull(bleInfo.Names);

            // Test the Ditto
            var inter = new TestBluetoothDeviceInterface("Vibratissimo", 2);
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
            Assert.AreEqual(1, dev.GetMessageAttrs(typeof(VibrateCmd)).FeatureCount);

            Assert.True(dev.ParseMessage(new StopDeviceCmd(4, 4)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(0, inter.LastWritten.Count);

            Assert.True(dev.ParseMessage(new SingleMotorVibrateCmd(4, 0.5, 6)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(2, inter.LastWritten.Count);
            Assert.AreEqual(6, inter.LastWritten[0].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)VibratissimoBluetoothInfo.Chrs.TxMode],
                inter.LastWritten[0].Characteristic);
            Assert.AreEqual(new byte[] { 0x03, 0xff }, inter.LastWritten[0].Value);
            Assert.False(inter.LastWritten[0].WriteWithResponse);
            Assert.AreEqual(6, inter.LastWritten[1].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)VibratissimoBluetoothInfo.Chrs.TxSpeed],
                inter.LastWritten[1].Characteristic);
            Assert.AreEqual(new byte[] { 0x80, 0x00 }, inter.LastWritten[1].Value);
            Assert.False(inter.LastWritten[1].WriteWithResponse);
            inter.LastWritten.Clear();

            Assert.True(dev.ParseMessage(new SingleMotorVibrateCmd(4, 0.5, 6)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(0, inter.LastWritten.Count);

            Assert.True(dev.ParseMessage(new SingleMotorVibrateCmd(4, 1, 6)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(2, inter.LastWritten.Count);
            Assert.AreEqual(6, inter.LastWritten[0].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)VibratissimoBluetoothInfo.Chrs.TxMode],
                inter.LastWritten[0].Characteristic);
            Assert.AreEqual(new byte[] { 0x03, 0xff }, inter.LastWritten[0].Value);
            Assert.False(inter.LastWritten[0].WriteWithResponse);
            Assert.AreEqual(6, inter.LastWritten[1].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)VibratissimoBluetoothInfo.Chrs.TxSpeed],
                inter.LastWritten[1].Characteristic);
            Assert.AreEqual(new byte[] { 0xff, 0x00 }, inter.LastWritten[1].Value);
            Assert.False(inter.LastWritten[1].WriteWithResponse);
            inter.LastWritten.Clear();

            Assert.True(dev.ParseMessage(new SingleMotorVibrateCmd(4, 0.25, 6)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(2, inter.LastWritten.Count);
            Assert.AreEqual(6, inter.LastWritten[0].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)VibratissimoBluetoothInfo.Chrs.TxMode],
                inter.LastWritten[0].Characteristic);
            Assert.AreEqual(new byte[] { 0x03, 0xff }, inter.LastWritten[0].Value);
            Assert.False(inter.LastWritten[0].WriteWithResponse);
            Assert.AreEqual(6, inter.LastWritten[1].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)VibratissimoBluetoothInfo.Chrs.TxSpeed],
                inter.LastWritten[1].Characteristic);
            Assert.AreEqual(new byte[] { 0x40, 0x00 }, inter.LastWritten[1].Value);
            Assert.False(inter.LastWritten[1].WriteWithResponse);
            inter.LastWritten.Clear();

            Assert.True(dev.ParseMessage(new StopDeviceCmd(4, 9)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(2, inter.LastWritten.Count);
            Assert.AreEqual(9, inter.LastWritten[0].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)VibratissimoBluetoothInfo.Chrs.TxMode],
                inter.LastWritten[0].Characteristic);
            Assert.AreEqual(new byte[] { 0x03, 0xff }, inter.LastWritten[0].Value);
            Assert.False(inter.LastWritten[0].WriteWithResponse);
            Assert.AreEqual(9, inter.LastWritten[1].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)VibratissimoBluetoothInfo.Chrs.TxSpeed],
                inter.LastWritten[1].Characteristic);
            Assert.AreEqual(new byte[] { 0x00, 0x00 }, inter.LastWritten[1].Value);
            Assert.False(inter.LastWritten[1].WriteWithResponse);
            inter.LastWritten.Clear();

            Assert.True(dev.ParseMessage(new VibrateCmd(4,
                new List<VibrateCmd.VibrateSubcommand>
                {
                    new VibrateCmd.VibrateSubcommand(0, 0.75),
                    new VibrateCmd.VibrateSubcommand(1, 0.75),
                }, 8)).GetAwaiter().GetResult() is Error);
            Assert.True(dev.ParseMessage(new VibrateCmd(4,
                new List<VibrateCmd.VibrateSubcommand>
                {
                    new VibrateCmd.VibrateSubcommand(1, 0.75),
                }, 8)).GetAwaiter().GetResult() is Error);
        }
    }
}
