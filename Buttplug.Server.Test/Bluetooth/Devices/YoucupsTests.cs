using System.Collections.Generic;
using System.Linq;
using System.Text;
using Buttplug.Core;
using Buttplug.Core.Messages;
using Buttplug.Server.Bluetooth.Devices;
using NUnit.Framework;

namespace Buttplug.Server.Test.Bluetooth.Devices
{
    /*
    [TestFixture]
    public class YoucupsTests
    {
        [Test]
        public void YoucupsTest()
        {
            var bleInfo = new YoucupsBluetoothInfo();

            Assert.NotNull(bleInfo.Services);
            Assert.True(bleInfo.Services.Any());
            Assert.NotNull(bleInfo.Services[0]);

            Assert.NotNull(bleInfo.Names);

            // Test the Warrior II
            var inter = new TestBluetoothDeviceInterface("Youcups", 2);
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
            Assert.AreEqual(1, inter.LastWritten.Count);
            Assert.AreEqual(6, inter.LastWritten[0].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)YoucupsBluetoothInfo.Chrs.Tx],
                inter.LastWritten[0].Characteristic);
            Assert.AreEqual("$SYS,4?", Encoding.ASCII.GetString(inter.LastWritten[0].Value));
            Assert.False(inter.LastWritten[0].WriteWithResponse);
            inter.LastWritten.Clear();

            Assert.True(dev.ParseMessage(new SingleMotorVibrateCmd(4, 0.5, 6)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(0, inter.LastWritten.Count);

            Assert.True(dev.ParseMessage(new SingleMotorVibrateCmd(4, 1, 6)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(1, inter.LastWritten.Count);
            Assert.AreEqual(6, inter.LastWritten[0].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)YoucupsBluetoothInfo.Chrs.Tx],
                inter.LastWritten[0].Characteristic);
            Assert.AreEqual("$SYS,8?", Encoding.ASCII.GetString(inter.LastWritten[0].Value));
            Assert.False(inter.LastWritten[0].WriteWithResponse);
            inter.LastWritten.Clear();

            Assert.True(dev.ParseMessage(new SingleMotorVibrateCmd(4, 0.25, 6)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(1, inter.LastWritten.Count);
            Assert.AreEqual(6, inter.LastWritten[0].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)YoucupsBluetoothInfo.Chrs.Tx],
                inter.LastWritten[0].Characteristic);
            Assert.AreEqual("$SYS,2?", Encoding.ASCII.GetString(inter.LastWritten[0].Value));
            Assert.False(inter.LastWritten[0].WriteWithResponse);
            inter.LastWritten.Clear();

            Assert.True(dev.ParseMessage(new StopDeviceCmd(4, 9)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(1, inter.LastWritten.Count);
            Assert.AreEqual(9, inter.LastWritten[0].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)YoucupsBluetoothInfo.Chrs.Tx],
                inter.LastWritten[0].Characteristic);
            Assert.AreEqual("$SYS,0?", Encoding.ASCII.GetString(inter.LastWritten[0].Value));
            Assert.False(inter.LastWritten[0].WriteWithResponse);
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
    */
}
