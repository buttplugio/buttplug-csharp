using System.Collections.Generic;
using System.Linq;
using Buttplug.Core;
using Buttplug.Core.Messages;
using Buttplug.Server.Bluetooth.Devices;
using NUnit.Framework;

namespace Buttplug.Server.Test.Bluetooth.Devices
{
    [TestFixture]
    public class VorzeA10CycloneTests
    {
        [Test]
        public void VorzeA10CycloneTest()
        {
            var bleInfo = new VorzeA10CycloneInfo();

            Assert.True(bleInfo.Characteristics.Length > (uint)VorzeA10CycloneInfo.Chrs.Tx);
            Assert.NotNull(bleInfo.Characteristics[(uint)VorzeA10CycloneInfo.Chrs.Tx]);

            Assert.NotNull(bleInfo.Services);
            Assert.True(bleInfo.Services.Any());
            Assert.NotNull(bleInfo.Services[0]);

            Assert.NotNull(bleInfo.Names);

            // Test the Ditto
            var inter = new TestBluetoothDeviceInterface("CycSA", 2);
            var dev = bleInfo.CreateDevice(new ButtplugLogManager(), inter);
            Assert.AreEqual(3, dev.GetAllowedMessageTypes().Count());
            Assert.True(dev.GetAllowedMessageTypes().Contains(typeof(StopDeviceCmd)));
            Assert.NotNull(dev.GetMessageAttrs(typeof(StopDeviceCmd)));
            Assert.Null(dev.GetMessageAttrs(typeof(StopDeviceCmd)).FeatureCount);
            Assert.True(dev.GetAllowedMessageTypes().Contains(typeof(VorzeA10CycloneCmd)));
            Assert.NotNull(dev.GetMessageAttrs(typeof(VorzeA10CycloneCmd)));
            Assert.Null(dev.GetMessageAttrs(typeof(VorzeA10CycloneCmd)).FeatureCount);
            Assert.True(dev.GetAllowedMessageTypes().Contains(typeof(RotateCmd)));
            Assert.NotNull(dev.GetMessageAttrs(typeof(RotateCmd)));
            Assert.AreEqual(1, dev.GetMessageAttrs(typeof(RotateCmd)).FeatureCount);

            Assert.True(dev.ParseMessage(new StopDeviceCmd(4, 4)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(0, inter.LastWriten.Count);

            Assert.True(dev.ParseMessage(new VorzeA10CycloneCmd(4, 50, true, 6)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(1, inter.LastWriten.Count);
            Assert.AreEqual(6, inter.LastWriten[0].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)VorzeA10CycloneInfo.Chrs.Tx],
                inter.LastWriten[0].Characteristic);
            Assert.AreEqual(new byte[] { 0x01, 0x01, 0xb2 }, inter.LastWriten[0].Value);
            Assert.False(inter.LastWriten[0].WriteWithResponse);
            inter.LastWriten.Clear();

            Assert.True(dev.ParseMessage(new VorzeA10CycloneCmd(4, 50, true, 6)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(0, inter.LastWriten.Count);

            Assert.True(dev.ParseMessage(new VorzeA10CycloneCmd(4, 99, true, 6)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(1, inter.LastWriten.Count);
            Assert.AreEqual(6, inter.LastWriten[0].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)VorzeA10CycloneInfo.Chrs.Tx],
                inter.LastWriten[0].Characteristic);
            Assert.AreEqual(new byte[] { 0x01, 0x01, 0xe3 }, inter.LastWriten[0].Value);
            Assert.False(inter.LastWriten[0].WriteWithResponse);
            inter.LastWriten.Clear();

            Assert.True(dev.ParseMessage(new VorzeA10CycloneCmd(4, 25, false, 6)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(1, inter.LastWriten.Count);
            Assert.AreEqual(6, inter.LastWriten[0].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)VorzeA10CycloneInfo.Chrs.Tx],
                inter.LastWriten[0].Characteristic);
            Assert.AreEqual(new byte[] { 0x01, 0x01, 0x19 }, inter.LastWriten[0].Value);
            Assert.False(inter.LastWriten[0].WriteWithResponse);
            inter.LastWriten.Clear();

            Assert.True(dev.ParseMessage(new StopDeviceCmd(4, 9)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(1, inter.LastWriten.Count);
            Assert.AreEqual(9, inter.LastWriten[0].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)VorzeA10CycloneInfo.Chrs.Tx],
                inter.LastWriten[0].Characteristic);
            Assert.AreEqual(new byte[] { 0x01, 0x01, 0x00 }, inter.LastWriten[0].Value);
            Assert.False(inter.LastWriten[0].WriteWithResponse);
            inter.LastWriten.Clear();

            Assert.True(dev.ParseMessage(new RotateCmd(4,
                new List<RotateCmd.RotateSubcommand>
                {
                    new RotateCmd.RotateSubcommand(0, 0.75, true),
                    new RotateCmd.RotateSubcommand(1, 0.75, false),
                }, 8)).GetAwaiter().GetResult() is Error);
            Assert.True(dev.ParseMessage(new RotateCmd(4,
                new List<RotateCmd.RotateSubcommand>
                {
                    new RotateCmd.RotateSubcommand(1, 0.75, false),
                }, 8)).GetAwaiter().GetResult() is Error);
        }
    }
}
