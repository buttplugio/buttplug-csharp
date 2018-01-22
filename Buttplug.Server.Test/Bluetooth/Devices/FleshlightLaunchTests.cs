using System.Collections.Generic;
using System.Linq;
using Buttplug.Core;
using Buttplug.Core.Messages;
using Buttplug.Server.Bluetooth.Devices;
using NUnit.Framework;

namespace Buttplug.Server.Test.Bluetooth.Devices
{
    [TestFixture]
    public class FleshlightLaunchTests
    {
        [Test]
        public void FleshlightLaunchTest()
        {
            var bleInfo = new FleshlightLaunchBluetoothInfo();

            foreach (var chr in new[]
            {
                FleshlightLaunchBluetoothInfo.Chrs.Rx,
                FleshlightLaunchBluetoothInfo.Chrs.Tx,
                FleshlightLaunchBluetoothInfo.Chrs.Cmd,
            })
            {
                Assert.True(bleInfo.Characteristics.Length > (uint)chr);
                Assert.NotNull(bleInfo.Characteristics[(uint)chr]);
            }

            Assert.NotNull(bleInfo.Services);
            Assert.True(bleInfo.Services.Any());
            Assert.NotNull(bleInfo.Services[0]);

            Assert.NotNull(bleInfo.Names);

            // Test the Launch
            var inter = new TestBluetoothDeviceInterface("Launch", 2);
            var dev = bleInfo.CreateDevice(new ButtplugLogManager(), inter);
            Assert.AreEqual(3, dev.GetAllowedMessageTypes().Count());
            Assert.True(dev.GetAllowedMessageTypes().Contains(typeof(StopDeviceCmd)));
            Assert.NotNull(dev.GetMessageAttrs(typeof(StopDeviceCmd)));
            Assert.Null(dev.GetMessageAttrs(typeof(StopDeviceCmd)).FeatureCount);
            Assert.True(dev.GetAllowedMessageTypes().Contains(typeof(FleshlightLaunchFW12Cmd)));
            Assert.NotNull(dev.GetMessageAttrs(typeof(FleshlightLaunchFW12Cmd)));
            Assert.Null(dev.GetMessageAttrs(typeof(FleshlightLaunchFW12Cmd)).FeatureCount);
            Assert.True(dev.GetAllowedMessageTypes().Contains(typeof(LinearCmd)));
            Assert.NotNull(dev.GetMessageAttrs(typeof(LinearCmd)));
            Assert.AreEqual(1, dev.GetMessageAttrs(typeof(LinearCmd)).FeatureCount);

            Assert.True(dev.Initialize().GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(1, inter.LastWriten.Count);
            Assert.AreEqual(0, inter.LastWriten[0].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)FleshlightLaunchBluetoothInfo.Chrs.Cmd],
                inter.LastWriten[0].Characteristic);
            Assert.AreEqual(new byte[] { 0x00 }, inter.LastWriten[0].Value);
            Assert.True(inter.LastWriten[0].WriteWithResponse);
            inter.LastWriten.Clear();

            Assert.True(dev.ParseMessage(new StopDeviceCmd(4, 4)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(0, inter.LastWriten.Count);

            Assert.True(dev.ParseMessage(new FleshlightLaunchFW12Cmd(4, 50, 50, 6)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(1, inter.LastWriten.Count);
            Assert.AreEqual(6, inter.LastWriten[0].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)FleshlightLaunchBluetoothInfo.Chrs.Tx],
                inter.LastWriten[0].Characteristic);
            Assert.AreEqual(new byte[] { 0x32, 0x32 }, inter.LastWriten[0].Value);
            Assert.False(inter.LastWriten[0].WriteWithResponse);
            inter.LastWriten.Clear();

            Assert.True(dev.ParseMessage(new FleshlightLaunchFW12Cmd(4, 50, 50, 6)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(1, inter.LastWriten.Count);
            Assert.AreEqual(6, inter.LastWriten[0].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)FleshlightLaunchBluetoothInfo.Chrs.Tx],
                inter.LastWriten[0].Characteristic);
            Assert.AreEqual(new byte[] { 0x32, 0x32 }, inter.LastWriten[0].Value);
            Assert.False(inter.LastWriten[0].WriteWithResponse);
            inter.LastWriten.Clear();

            Assert.True(dev.ParseMessage(new FleshlightLaunchFW12Cmd(4, 99, 99, 6)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(1, inter.LastWriten.Count);
            Assert.AreEqual(6, inter.LastWriten[0].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)FleshlightLaunchBluetoothInfo.Chrs.Tx],
                inter.LastWriten[0].Characteristic);
            Assert.AreEqual(new byte[] { 0x63, 0x63 }, inter.LastWriten[0].Value);
            Assert.False(inter.LastWriten[0].WriteWithResponse);
            inter.LastWriten.Clear();

            Assert.True(dev.ParseMessage(new LinearCmd(4, new List<LinearCmd.VectorSubcommands>
            {
                new LinearCmd.VectorSubcommands(0, 500, 0),
            }, 6)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(1, inter.LastWriten.Count);
            Assert.AreEqual(6, inter.LastWriten[0].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)FleshlightLaunchBluetoothInfo.Chrs.Tx],
                inter.LastWriten[0].Characteristic);
            Assert.AreEqual(new byte[] { 0x00, 0x29 }, inter.LastWriten[0].Value);
            Assert.False(inter.LastWriten[0].WriteWithResponse);
            inter.LastWriten.Clear();

            Assert.True(dev.ParseMessage(new StopDeviceCmd(4, 9)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(0, inter.LastWriten.Count);

            Assert.True(dev.ParseMessage(new LinearCmd(4,
                new List<LinearCmd.VectorSubcommands>
                {
                    new LinearCmd.VectorSubcommands(0, 500, 0.75),
                    new LinearCmd.VectorSubcommands(1, 500, 0.75),
                }, 8)).GetAwaiter().GetResult() is Error);
            Assert.True(dev.ParseMessage(new LinearCmd(4,
                new List<LinearCmd.VectorSubcommands>
                {
                    new LinearCmd.VectorSubcommands(1, 500, 0.75),
                }, 8)).GetAwaiter().GetResult() is Error);
        }
    }
}
