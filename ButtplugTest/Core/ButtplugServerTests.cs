using Buttplug.Core;
using Buttplug.Messages;
using System.Linq;
using Xunit;
using static Buttplug.Messages.Error;

namespace ButtplugTest.Core
{
    public class ButtplugServerTests
    {
        [Fact]
        public async void RejectOutgoingOnlyMessage()
        {
            Assert.True((await new TestService().SendMessage(new Error("Error", ErrorClass.ERROR_UNKNOWN, ButtplugConsts.DEFAULT_MSG_ID))) is Error);
        }

        [Fact]
        public async void LoggerSettingsTest()
        {
            var gotMessage = false;
            var s = new TestService();
            s.MessageReceived += (obj, msg) =>
            {
                if (msg.Message.GetType() == typeof(Log))
                {
                    gotMessage = true;
                }
            };
            // Sending error messages will always cause an error, as they are outgoing, not incoming.
            await s.SendMessage(new Error("Error", ErrorClass.ERROR_UNKNOWN, ButtplugConsts.DEFAULT_MSG_ID));
            Assert.False(gotMessage);
            Assert.True((await s.SendMessage(new RequestLog("Trace"))) is Ok);
            Assert.True((await s.SendMessage(new Error("Error", ErrorClass.ERROR_UNKNOWN, ButtplugConsts.DEFAULT_MSG_ID))) is Error);
            Assert.True(gotMessage);
            await s.SendMessage(new RequestLog("Off"));
            gotMessage = false;
            await s.SendMessage(new Error("Error", ErrorClass.ERROR_UNKNOWN, ButtplugConsts.DEFAULT_MSG_ID));
            Assert.False(gotMessage);
        }

        [Fact]
        public async void CheckMessageReturnId()
        {
            var s = new TestService();
            s.MessageReceived += (obj, msg) =>
            {
                Assert.True(msg.Message is RequestServerInfo);
                Assert.True(msg.Message.Id == 12345);
            };
            var m = new RequestServerInfo("TestClient", 12345);
            await s.SendMessage(m);
            await s.SendMessage("{\"RequestServerInfo\":{\"Id\":12345}}");
        }

        public void CheckDeviceMessages(ButtplugMessage msgArgs)
        {
            switch (msgArgs)
            {
                case DeviceAdded da:                        
                    Assert.True(da.DeviceName == "TestDevice");
                    Assert.True(da.DeviceIndex == 1);
                    Assert.True(da.DeviceMessages.Length == 1);
                    Assert.True(da.DeviceMessages.Contains("SingleMotorVibrateCmd"));
                    break;
                case DeviceList dl:
                    Assert.True(dl.Devices.Length == 1);
                    var di = dl.Devices[0];
                    Assert.True(di.DeviceName == "TestDevice");
                    Assert.True(di.DeviceIndex == 1);
                    Assert.True(di.DeviceMessages.Length == 1);
                    Assert.True(di.DeviceMessages.Contains("SingleMotorVibrateCmd"));
                    break;
                case DeviceRemoved dr:                        
                    Assert.True(dr.DeviceIndex == 1);
                    break;
                default:
                    Assert.True(false, $"Shouldn't be here {msgArgs.GetType().Name}");
                    break;
            }
        }

        public async void CheckDeviceCount(ButtplugService s, int expectedCount)
        {
            var deviceListMsg = await s.SendMessage(new RequestDeviceList());
            Assert.True(deviceListMsg is DeviceList);
            Assert.Equal(((DeviceList)deviceListMsg).Devices.Count(), expectedCount);
        }

        [Fact]
        public async void TestAddListRemoveDevices()
        {
            var d = new TestDevice(new ButtplugLogManager(), "TestDevice");
            var msgarray = d.GetAllowedMessageTypes();
            Assert.True(msgarray.Count() == 1);
            Assert.True(msgarray.Contains(typeof(SingleMotorVibrateCmd)));
            var m = new TestDeviceSubtypeManager(new ButtplugLogManager(), d);
            var s = new TestService();
            s.AddDeviceSubtypeManager(m);
            ButtplugMessage msgReceived = null;
            s.MessageReceived += (obj, msgArgs) =>
            {
                msgReceived = msgArgs.Message;
                CheckDeviceMessages(msgReceived);
            };
            CheckDeviceCount(s, 0);
            Assert.True(await s.SendMessage(new StartScanning()) is Ok);
            Assert.True(await s.SendMessage(new StopScanning()) is Ok);
            Assert.True(msgReceived is DeviceAdded);
            CheckDeviceCount(s, 1);
            msgReceived = await s.SendMessage(new RequestDeviceList());
            d.RemoveDevice();
            Assert.True(msgReceived is DeviceRemoved);
            CheckDeviceCount(s, 0);
        }

        [Fact]
        public async void TestIncomingSystemIdMessage()
        {
            var s = new TestService();
            // Test echos back a test message with the same string and id
            Assert.True(await s.SendMessage(new Test("Right", 1)) is Test);
            Assert.True(await s.SendMessage(new Test("Wrong", 0)) is Error);
        }

        [Fact]
        public async void TestInvalidDeviceIdMessage()
        {
            var s = new TestService();
            Assert.True((await s.SendMessage(new SingleMotorVibrateCmd(1, .2, 0))) is Error);
        }

        [Fact]
        public async void TestValidDeviceMessage()
        {
            var d = new TestDevice(new ButtplugLogManager(), "TestDevice");
            var m = new TestDeviceSubtypeManager(new ButtplugLogManager(), d);
            var s = new TestService();
            s.AddDeviceSubtypeManager(m);
            Assert.True(await s.SendMessage(new StartScanning()) is Ok);
            Assert.True(await s.SendMessage(new StopScanning()) is Ok);
            Assert.True(await s.SendMessage(new SingleMotorVibrateCmd(1, .2)) is Ok);
        }

        [Fact]
        public async void TestInvalidDeviceMessage()
        {
            var d = new TestDevice(new ButtplugLogManager(), "TestDevice");
            var m = new TestDeviceSubtypeManager(new ButtplugLogManager(), d);
            var s = new TestService();
            s.AddDeviceSubtypeManager(m);
            Assert.True(await s.SendMessage(new StartScanning()) is Ok);
            Assert.True(await s.SendMessage(new StopScanning()) is Ok);
            Assert.True(await s.SendMessage(new FleshlightLaunchFW12Cmd(1, 0, 0)) is Error);
        }

        [Fact]
        public async void TestDuplicateDeviceAdded()
        {
            var d = new TestDevice(new ButtplugLogManager(), "TestDevice");
            var m = new TestDeviceSubtypeManager(new ButtplugLogManager(), d);
            var s = new TestService();
            s.AddDeviceSubtypeManager(m);
            var msgReceived = false;
            s.MessageReceived += (obj, msgArgs) =>
            {
                msgReceived = true;
                switch (msgArgs.Message)
                {
                    case DeviceAdded da:
                        Assert.True(da.DeviceName == "TestDevice");
                        Assert.True(da.DeviceIndex == 1);
                        Assert.True(da.Id == 0);
                        break;

                    default:
                        Assert.False(msgArgs.Message is DeviceAdded);
                        break;
                }
            };
            for (var i = 0; i < 2; ++i)
            {
                Assert.True((await s.SendMessage(new StartScanning())) is Ok);
                Assert.True((await s.SendMessage(new StopScanning())) is Ok);
                var x = await s.SendMessage(new RequestDeviceList());
                Assert.True(x is DeviceList);
                switch (x)
                {
                    case DeviceList dl:
                        Assert.True(dl.Devices.Count() == 1);
                        Assert.True(dl.Devices[0].DeviceIndex == 1);
                        Assert.True(dl.Devices[0].DeviceName == "TestDevice");
                        break;
                }
                Assert.True(i == 0 ? msgReceived : !msgReceived, "DeviceAdded fired at incorrect time!");
                msgReceived = false;
            }
        }
    }
}