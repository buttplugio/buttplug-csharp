using System.Collections.Generic;
using System.Linq;
using System.Text;
using Buttplug.Core;
using Buttplug.Core.Messages;
using Buttplug.Server.Bluetooth.Devices;
using NUnit.Framework;

namespace Buttplug.Server.Test.Bluetooth.Devices
{
    [TestFixture]
    public class LovenseTests
    {
        [Test]
        public void LovenseV1Test()
        {
            var bleInfo = new LovenseRev1BluetoothInfo();

            foreach (var chr in new[]
            {
                LovenseRev1BluetoothInfo.Chrs.Rx,
                LovenseRev1BluetoothInfo.Chrs.Tx,
            })
            {
                Assert.True(bleInfo.Characteristics.Length > (uint)chr);
                Assert.NotNull(bleInfo.Characteristics[(uint)chr]);
            }

            Assert.NotNull(bleInfo.Services);
            Assert.True(bleInfo.Services.Any());
            Assert.NotNull(bleInfo.Services[0]);

            Assert.NotNull(bleInfo.Names);

            // Test the Nora
            var inter = new TestBluetoothDeviceInterface("LVS-A011", 2);
            var dev = bleInfo.CreateDevice(new ButtplugLogManager(), inter);
            Assert.AreEqual(4, dev.GetAllowedMessageTypes().Count());
            Assert.True(dev.GetAllowedMessageTypes().Contains(typeof(StopDeviceCmd)));
            Assert.NotNull(dev.GetMessageAttrs(typeof(StopDeviceCmd)));
            Assert.Null(dev.GetMessageAttrs(typeof(StopDeviceCmd)).FeatureCount);
            Assert.True(dev.GetAllowedMessageTypes().Contains(typeof(SingleMotorVibrateCmd)));
            Assert.NotNull(dev.GetMessageAttrs(typeof(SingleMotorVibrateCmd)));
            Assert.Null(dev.GetMessageAttrs(typeof(SingleMotorVibrateCmd)).FeatureCount);
            Assert.True(dev.GetAllowedMessageTypes().Contains(typeof(VibrateCmd)));
            Assert.NotNull(dev.GetMessageAttrs(typeof(VibrateCmd)));
            Assert.AreEqual(1, dev.GetMessageAttrs(typeof(VibrateCmd)).FeatureCount);
            Assert.True(dev.GetAllowedMessageTypes().Contains(typeof(RotateCmd)));
            Assert.NotNull(dev.GetMessageAttrs(typeof(RotateCmd)));
            Assert.AreEqual(1, dev.GetMessageAttrs(typeof(RotateCmd)).FeatureCount);

            Assert.True(dev.ParseMessage(new StopDeviceCmd(4, 4)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(0, inter.LastWriten.Count);

            Assert.True(dev.ParseMessage(new SingleMotorVibrateCmd(4, 0.5, 6)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(1, inter.LastWriten.Count);
            Assert.AreEqual(6, inter.LastWriten[0].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)LovenseRev1BluetoothInfo.Chrs.Tx], inter.LastWriten[0].Characteristic);
            Assert.AreEqual(Encoding.ASCII.GetBytes("Vibrate:10;"), inter.LastWriten[0].Value);
            Assert.False(inter.LastWriten[0].WriteWithResponse);
            inter.LastWriten.Clear();

            Assert.True(dev.ParseMessage(new RotateCmd(4, new List<RotateCmd.RotateSubcommand> { new RotateCmd.RotateSubcommand(0, 0.75, false) }, 8)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(2, inter.LastWriten.Count);
            Assert.AreEqual(8, inter.LastWriten[0].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)LovenseRev1BluetoothInfo.Chrs.Tx], inter.LastWriten[0].Characteristic);
            Assert.AreEqual(Encoding.ASCII.GetBytes("RotateChange;"), inter.LastWriten[0].Value);
            Assert.False(inter.LastWriten[0].WriteWithResponse);
            Assert.AreEqual(8, inter.LastWriten[1].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)LovenseRev1BluetoothInfo.Chrs.Tx], inter.LastWriten[1].Characteristic);
            Assert.AreEqual(Encoding.ASCII.GetBytes("Rotate:15;"), inter.LastWriten[1].Value);
            Assert.False(inter.LastWriten[1].WriteWithResponse);
            inter.LastWriten.Clear();

            Assert.True(dev.ParseMessage(new StopDeviceCmd(4, 9)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(2, inter.LastWriten.Count);
            Assert.AreEqual(9, inter.LastWriten[0].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)LovenseRev1BluetoothInfo.Chrs.Tx], inter.LastWriten[0].Characteristic);
            Assert.AreEqual(Encoding.ASCII.GetBytes("Rotate:0;"), inter.LastWriten[0].Value);
            Assert.False(inter.LastWriten[0].WriteWithResponse);
            Assert.AreEqual(9, inter.LastWriten[1].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)LovenseRev1BluetoothInfo.Chrs.Tx], inter.LastWriten[1].Characteristic);
            Assert.AreEqual(Encoding.ASCII.GetBytes("Vibrate:0;"), inter.LastWriten[1].Value);
            Assert.False(inter.LastWriten[1].WriteWithResponse);
            inter.LastWriten.Clear();

            Assert.True(dev.ParseMessage(new RotateCmd(4,
                new List<RotateCmd.RotateSubcommand>
                {
                    new RotateCmd.RotateSubcommand(0, 0.75, false),
                    new RotateCmd.RotateSubcommand(1, 0.75, false),
                }, 8)).GetAwaiter().GetResult() is Error);
            Assert.True(dev.ParseMessage(new RotateCmd(4,
                new List<RotateCmd.RotateSubcommand>
                {
                    new RotateCmd.RotateSubcommand(1, 0.75, false),
                }, 8)).GetAwaiter().GetResult() is Error);

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

        [Test]
        public void LovenseV2Test()
        {
            var bleInfo = new LovenseRev2BluetoothInfo();

            foreach (var chr in new[]
            {
                LovenseRev2BluetoothInfo.Chrs.Rx,
                LovenseRev2BluetoothInfo.Chrs.Tx,
            })
            {
                Assert.True(bleInfo.Characteristics.Length > (uint)chr);
                Assert.NotNull(bleInfo.Characteristics[(uint)chr]);
            }

            Assert.NotNull(bleInfo.Services);
            Assert.True(bleInfo.Services.Any());
            Assert.NotNull(bleInfo.Services[0]);

            Assert.NotNull(bleInfo.Names);

            // Test the Hush
            var inter = new TestBluetoothDeviceInterface("LVS-Z001", 2);
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
            Assert.AreEqual(0, inter.LastWriten.Count);

            Assert.True(dev.ParseMessage(new SingleMotorVibrateCmd(4, 0.5, 6)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(1, inter.LastWriten.Count);
            Assert.AreEqual(6, inter.LastWriten[0].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)LovenseRev2BluetoothInfo.Chrs.Tx], inter.LastWriten[0].Characteristic);
            Assert.AreEqual(Encoding.ASCII.GetBytes("Vibrate:10;"), inter.LastWriten[0].Value);
            Assert.False(inter.LastWriten[0].WriteWithResponse);
            inter.LastWriten.Clear();

            Assert.True(dev.ParseMessage(new StopDeviceCmd(4, 9)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(1, inter.LastWriten.Count);
            Assert.AreEqual(9, inter.LastWriten[0].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)LovenseRev2BluetoothInfo.Chrs.Tx], inter.LastWriten[0].Characteristic);
            Assert.AreEqual(Encoding.ASCII.GetBytes("Vibrate:0;"), inter.LastWriten[0].Value);
            Assert.False(inter.LastWriten[0].WriteWithResponse);
            inter.LastWriten.Clear();

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

        [Test]
        public void LovenseV3Test()
        {
            var bleInfo = new LovenseRev3BluetoothInfo();

            foreach (var chr in new[]
            {
                LovenseRev3BluetoothInfo.Chrs.Rx,
                LovenseRev3BluetoothInfo.Chrs.Tx,
            })
            {
                Assert.True(bleInfo.Characteristics.Length > (uint)chr);
                Assert.NotNull(bleInfo.Characteristics[(uint)chr]);
            }

            Assert.NotNull(bleInfo.Services);
            Assert.True(bleInfo.Services.Any());
            Assert.NotNull(bleInfo.Services[0]);

            Assert.NotNull(bleInfo.Names);

            // Test the Edge
            var inter = new TestBluetoothDeviceInterface("LVS-P36", 2);
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
            Assert.AreEqual(2, inter.LastWriten.Count);
            Assert.AreEqual(6, inter.LastWriten[0].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)LovenseRev3BluetoothInfo.Chrs.Tx], inter.LastWriten[0].Characteristic);
            Assert.AreEqual("Vibrate1:10;", Encoding.ASCII.GetString(inter.LastWriten[0].Value));
            Assert.False(inter.LastWriten[0].WriteWithResponse);
            Assert.AreEqual(6, inter.LastWriten[1].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)LovenseRev3BluetoothInfo.Chrs.Tx], inter.LastWriten[1].Characteristic);
            Assert.AreEqual("Vibrate2:10;", Encoding.ASCII.GetString(inter.LastWriten[1].Value));
            Assert.False(inter.LastWriten[1].WriteWithResponse);
            inter.LastWriten.Clear();

            Assert.True(dev.ParseMessage(new StopDeviceCmd(4, 9)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(2, inter.LastWriten.Count);
            Assert.AreEqual(9, inter.LastWriten[0].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)LovenseRev3BluetoothInfo.Chrs.Tx], inter.LastWriten[0].Characteristic);
            Assert.AreEqual("Vibrate1:0;", Encoding.ASCII.GetString(inter.LastWriten[0].Value));
            Assert.AreEqual(Encoding.ASCII.GetBytes("Vibrate1:0;"), inter.LastWriten[0].Value);
            Assert.False(inter.LastWriten[0].WriteWithResponse);
            Assert.AreEqual(9, inter.LastWriten[1].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)LovenseRev3BluetoothInfo.Chrs.Tx], inter.LastWriten[1].Characteristic);
            Assert.AreEqual("Vibrate2:0;", Encoding.ASCII.GetString(inter.LastWriten[1].Value));
            Assert.False(inter.LastWriten[1].WriteWithResponse);
            inter.LastWriten.Clear();

            Assert.True(dev.ParseMessage(new VibrateCmd(4,
                new List<VibrateCmd.VibrateSubcommand>
                {
                    new VibrateCmd.VibrateSubcommand(0, 0.75),
                }, 8)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(1, inter.LastWriten.Count);
            Assert.AreEqual(8, inter.LastWriten[0].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)LovenseRev3BluetoothInfo.Chrs.Tx], inter.LastWriten[0].Characteristic);
            Assert.AreEqual("Vibrate1:15;", Encoding.ASCII.GetString(inter.LastWriten[0].Value));
            Assert.False(inter.LastWriten[0].WriteWithResponse);
            inter.LastWriten.Clear();

            Assert.True(dev.ParseMessage(new VibrateCmd(4,
                new List<VibrateCmd.VibrateSubcommand>
                {
                    new VibrateCmd.VibrateSubcommand(1, 0.25),
                }, 8)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(1, inter.LastWriten.Count);
            Assert.AreEqual(8, inter.LastWriten[0].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)LovenseRev3BluetoothInfo.Chrs.Tx], inter.LastWriten[0].Characteristic);
            Assert.AreEqual("Vibrate2:5;", Encoding.ASCII.GetString(inter.LastWriten[0].Value));
            Assert.False(inter.LastWriten[0].WriteWithResponse);
            inter.LastWriten.Clear();

            Assert.True(dev.ParseMessage(new VibrateCmd(4,
                new List<VibrateCmd.VibrateSubcommand>
                {
                    new VibrateCmd.VibrateSubcommand(0, 0.25),
                    new VibrateCmd.VibrateSubcommand(1, 0),
                }, 8)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(2, inter.LastWriten.Count);
            Assert.AreEqual(8, inter.LastWriten[0].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)LovenseRev3BluetoothInfo.Chrs.Tx], inter.LastWriten[0].Characteristic);
            Assert.AreEqual("Vibrate1:5;", Encoding.ASCII.GetString(inter.LastWriten[0].Value));
            Assert.False(inter.LastWriten[0].WriteWithResponse);
            Assert.AreEqual(8, inter.LastWriten[1].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)LovenseRev3BluetoothInfo.Chrs.Tx], inter.LastWriten[1].Characteristic);
            Assert.AreEqual("Vibrate2:0;", Encoding.ASCII.GetString(inter.LastWriten[1].Value));
            Assert.False(inter.LastWriten[1].WriteWithResponse);
            inter.LastWriten.Clear();

            Assert.True(dev.ParseMessage(new VibrateCmd(4,
                new List<VibrateCmd.VibrateSubcommand>
                {
                    new VibrateCmd.VibrateSubcommand(0, 0.75),
                    new VibrateCmd.VibrateSubcommand(1, 0.75),
                    new VibrateCmd.VibrateSubcommand(3, 0.75),
                }, 8)).GetAwaiter().GetResult() is Error);
            Assert.True(dev.ParseMessage(new VibrateCmd(4,
                new List<VibrateCmd.VibrateSubcommand>
                {
                    new VibrateCmd.VibrateSubcommand(3, 0.75),
                }, 8)).GetAwaiter().GetResult() is Error);
        }

        [Test]
        public void LovenseV4Test()
        {
            var bleInfo = new LovenseRev4BluetoothInfo();

            foreach (var chr in new[]
            {
                LovenseRev4BluetoothInfo.Chrs.Rx,
                LovenseRev4BluetoothInfo.Chrs.Tx,
            })
            {
                Assert.True(bleInfo.Characteristics.Length > (uint)chr);
                Assert.NotNull(bleInfo.Characteristics[(uint)chr]);
            }

            Assert.NotNull(bleInfo.Services);
            Assert.True(bleInfo.Services.Any());
            Assert.NotNull(bleInfo.Services[0]);

            Assert.NotNull(bleInfo.Names);

            // Test the Domi
            var inter = new TestBluetoothDeviceInterface("LVS-Domi37", 2);
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
            Assert.AreEqual(0, inter.LastWriten.Count);

            Assert.True(dev.ParseMessage(new SingleMotorVibrateCmd(4, 0.5, 6)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(1, inter.LastWriten.Count);
            Assert.AreEqual(6, inter.LastWriten[0].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)LovenseRev4BluetoothInfo.Chrs.Tx], inter.LastWriten[0].Characteristic);
            Assert.AreEqual(Encoding.ASCII.GetBytes("Vibrate:10;"), inter.LastWriten[0].Value);
            Assert.False(inter.LastWriten[0].WriteWithResponse);
            inter.LastWriten.Clear();

            Assert.True(dev.ParseMessage(new StopDeviceCmd(4, 9)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(1, inter.LastWriten.Count);
            Assert.AreEqual(9, inter.LastWriten[0].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)LovenseRev4BluetoothInfo.Chrs.Tx], inter.LastWriten[0].Characteristic);
            Assert.AreEqual(Encoding.ASCII.GetBytes("Vibrate:0;"), inter.LastWriten[0].Value);
            Assert.False(inter.LastWriten[0].WriteWithResponse);
            inter.LastWriten.Clear();

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

        [Test]
        public void LovenseV5Test()
        {
            var bleInfo = new LovenseRev5BluetoothInfo();

            foreach (var chr in new[]
            {
                LovenseRev5BluetoothInfo.Chrs.Rx,
                LovenseRev5BluetoothInfo.Chrs.Tx,
            })
            {
                Assert.True(bleInfo.Characteristics.Length > (uint)chr);
                Assert.NotNull(bleInfo.Characteristics[(uint)chr]);
            }

            Assert.NotNull(bleInfo.Services);
            Assert.True(bleInfo.Services.Any());
            Assert.NotNull(bleInfo.Services[0]);

            Assert.NotNull(bleInfo.Names);

            // Test the Hush
            var inter = new TestBluetoothDeviceInterface("LVS-Z36", 2);
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
            Assert.AreEqual(0, inter.LastWriten.Count);

            Assert.True(dev.ParseMessage(new SingleMotorVibrateCmd(4, 0.5, 6)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(1, inter.LastWriten.Count);
            Assert.AreEqual(6, inter.LastWriten[0].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)LovenseRev5BluetoothInfo.Chrs.Tx], inter.LastWriten[0].Characteristic);
            Assert.AreEqual(Encoding.ASCII.GetBytes("Vibrate:10;"), inter.LastWriten[0].Value);
            Assert.False(inter.LastWriten[0].WriteWithResponse);
            inter.LastWriten.Clear();

            Assert.True(dev.ParseMessage(new StopDeviceCmd(4, 9)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(1, inter.LastWriten.Count);
            Assert.AreEqual(9, inter.LastWriten[0].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)LovenseRev5BluetoothInfo.Chrs.Tx], inter.LastWriten[0].Characteristic);
            Assert.AreEqual(Encoding.ASCII.GetBytes("Vibrate:0;"), inter.LastWriten[0].Value);
            Assert.False(inter.LastWriten[0].WriteWithResponse);
            inter.LastWriten.Clear();

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

        [Test]
        public void LovenseV6Test()
        {
            var bleInfo = new LovenseRev6BluetoothInfo();

            foreach (var chr in new[]
            {
                LovenseRev6BluetoothInfo.Chrs.Rx,
                LovenseRev6BluetoothInfo.Chrs.Tx,
            })
            {
                Assert.True(bleInfo.Characteristics.Length > (uint)chr);
                Assert.NotNull(bleInfo.Characteristics[(uint)chr]);
            }

            Assert.NotNull(bleInfo.Services);
            Assert.True(bleInfo.Services.Any());
            Assert.NotNull(bleInfo.Services[0]);

            Assert.NotNull(bleInfo.Names);

            // Test the Edge
            var inter = new TestBluetoothDeviceInterface("LVS-Edge37", 2);
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
            Assert.AreEqual(2, inter.LastWriten.Count);
            Assert.AreEqual(6, inter.LastWriten[0].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)LovenseRev6BluetoothInfo.Chrs.Tx], inter.LastWriten[0].Characteristic);
            Assert.AreEqual("Vibrate1:10;", Encoding.ASCII.GetString(inter.LastWriten[0].Value));
            Assert.False(inter.LastWriten[0].WriteWithResponse);
            Assert.AreEqual(6, inter.LastWriten[1].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)LovenseRev6BluetoothInfo.Chrs.Tx], inter.LastWriten[1].Characteristic);
            Assert.AreEqual("Vibrate2:10;", Encoding.ASCII.GetString(inter.LastWriten[1].Value));
            Assert.False(inter.LastWriten[1].WriteWithResponse);
            inter.LastWriten.Clear();

            Assert.True(dev.ParseMessage(new StopDeviceCmd(4, 9)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(2, inter.LastWriten.Count);
            Assert.AreEqual(9, inter.LastWriten[0].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)LovenseRev6BluetoothInfo.Chrs.Tx], inter.LastWriten[0].Characteristic);
            Assert.AreEqual("Vibrate1:0;", Encoding.ASCII.GetString(inter.LastWriten[0].Value));
            Assert.AreEqual(Encoding.ASCII.GetBytes("Vibrate1:0;"), inter.LastWriten[0].Value);
            Assert.False(inter.LastWriten[0].WriteWithResponse);
            Assert.AreEqual(9, inter.LastWriten[1].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)LovenseRev6BluetoothInfo.Chrs.Tx], inter.LastWriten[1].Characteristic);
            Assert.AreEqual("Vibrate2:0;", Encoding.ASCII.GetString(inter.LastWriten[1].Value));
            Assert.False(inter.LastWriten[1].WriteWithResponse);
            inter.LastWriten.Clear();

            Assert.True(dev.ParseMessage(new VibrateCmd(4,
                new List<VibrateCmd.VibrateSubcommand>
                {
                    new VibrateCmd.VibrateSubcommand(0, 0.75),
                }, 8)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(1, inter.LastWriten.Count);
            Assert.AreEqual(8, inter.LastWriten[0].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)LovenseRev6BluetoothInfo.Chrs.Tx], inter.LastWriten[0].Characteristic);
            Assert.AreEqual("Vibrate1:15;", Encoding.ASCII.GetString(inter.LastWriten[0].Value));
            Assert.False(inter.LastWriten[0].WriteWithResponse);
            inter.LastWriten.Clear();

            Assert.True(dev.ParseMessage(new VibrateCmd(4,
                new List<VibrateCmd.VibrateSubcommand>
                {
                    new VibrateCmd.VibrateSubcommand(1, 0.25),
                }, 8)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(1, inter.LastWriten.Count);
            Assert.AreEqual(8, inter.LastWriten[0].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)LovenseRev6BluetoothInfo.Chrs.Tx], inter.LastWriten[0].Characteristic);
            Assert.AreEqual("Vibrate2:5;", Encoding.ASCII.GetString(inter.LastWriten[0].Value));
            Assert.False(inter.LastWriten[0].WriteWithResponse);
            inter.LastWriten.Clear();

            Assert.True(dev.ParseMessage(new VibrateCmd(4,
                new List<VibrateCmd.VibrateSubcommand>
                {
                    new VibrateCmd.VibrateSubcommand(0, 0.25),
                    new VibrateCmd.VibrateSubcommand(1, 0),
                }, 8)).GetAwaiter().GetResult() is Ok);
            Assert.AreEqual(2, inter.LastWriten.Count);
            Assert.AreEqual(8, inter.LastWriten[0].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)LovenseRev6BluetoothInfo.Chrs.Tx], inter.LastWriten[0].Characteristic);
            Assert.AreEqual("Vibrate1:5;", Encoding.ASCII.GetString(inter.LastWriten[0].Value));
            Assert.False(inter.LastWriten[0].WriteWithResponse);
            Assert.AreEqual(8, inter.LastWriten[1].MsgId);
            Assert.AreEqual(bleInfo.Characteristics[(uint)LovenseRev6BluetoothInfo.Chrs.Tx], inter.LastWriten[1].Characteristic);
            Assert.AreEqual("Vibrate2:0;", Encoding.ASCII.GetString(inter.LastWriten[1].Value));
            Assert.False(inter.LastWriten[1].WriteWithResponse);
            inter.LastWriten.Clear();

            Assert.True(dev.ParseMessage(new VibrateCmd(4,
                new List<VibrateCmd.VibrateSubcommand>
                {
                    new VibrateCmd.VibrateSubcommand(0, 0.75),
                    new VibrateCmd.VibrateSubcommand(1, 0.75),
                    new VibrateCmd.VibrateSubcommand(3, 0.75),
                }, 8)).GetAwaiter().GetResult() is Error);
            Assert.True(dev.ParseMessage(new VibrateCmd(4,
                new List<VibrateCmd.VibrateSubcommand>
                {
                    new VibrateCmd.VibrateSubcommand(3, 0.75),
                }, 8)).GetAwaiter().GetResult() is Error);
        }
    }
}
