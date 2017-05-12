using Buttplug.Core;
using Buttplug.Messages;
using System.Linq;
using Xunit;

namespace ButtplugTest.Core
{
    public class ButtplugServerTests
    {
        [Fact]
        public async void RejectOutgoingOnlyMessage()
        {
            Assert.True((await new ButtplugService().SendMessage(new Error("Error", ButtplugConsts.DEFAULT_MSG_ID))) is Error);
        }

        [Fact]
        public async void LoggerSettingsTest()
        {
            var gotMessage = false;
            var s = new ButtplugService();
            s.MessageReceived += (obj, msg) =>
            {
                if (msg.Message.GetType() == typeof(Log))
                {
                    gotMessage = true;
                }
            };
            // Sending error messages will always cause an error, as they are outgoing, not incoming.
            await s.SendMessage(new Error("Error", ButtplugConsts.DEFAULT_MSG_ID));
            Assert.False(gotMessage);
            Assert.True((await s.SendMessage(new RequestLog("Trace"))) is Ok);
            Assert.True((await s.SendMessage(new Error("Error", ButtplugConsts.DEFAULT_MSG_ID))) is Error);
            Assert.True(gotMessage);
            await s.SendMessage(new RequestLog("Off"));
            gotMessage = false;
            await s.SendMessage(new Error("Error", ButtplugConsts.DEFAULT_MSG_ID));
            Assert.False(gotMessage);
        }

        [Fact]
        public async void CheckMessageReturnId()
        {
            var s = new ButtplugService();
            s.MessageReceived += (obj, msg) =>
            {
                Assert.True(msg.Message is RequestServerInfo);
                Assert.True(msg.Message.Id == 12345);
            };
            var m = new RequestServerInfo(12345);
            await s.SendMessage(m);
            await s.SendMessage("{\"RequestServerInfo\":{\"Id\":12345}}");
        }

        [Fact]
        public async void AddDeviceTest()
        {
            var d = new TestDevice("TestDevice");
            var m = new TestDeviceManager(d);
            var s = new TestService(m);
            var msgReceived = false;
            s.MessageReceived += (obj, msgArgs) =>
            {
                msgReceived = true;
                switch (msgArgs.Message)
                {
                    case DeviceAdded da:
                        Assert.True(da.DeviceName == "TestDevice");
                        Assert.True(da.DeviceIndex == 0);
                        break;

                    default:
                        Assert.False(msgArgs.Message is DeviceAdded);
                        break;
                }
            };
            Assert.True((await s.SendMessage(new StartScanning())) is Ok);
            Assert.True(msgReceived);
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
            var d = new TestDevice("TestDevice");
            var m = new TestDeviceManager(d);
            var s = new TestService(m);
            Assert.True(await s.SendMessage(new StartScanning()) is Ok);
            Assert.True(await s.SendMessage(new StopScanning()) is Ok);
            Assert.True(await s.SendMessage(new SingleMotorVibrateCmd(0, .2)) is Ok);
        }

        [Fact]
        public async void TestDuplicateDeviceAdded()
        {
            var d = new TestDevice("TestDevice");
            var m = new TestDeviceManager(d);
            var s = new TestService(m);
            var msgReceived = false;
            s.MessageReceived += (obj, msgArgs) =>
            {
                msgReceived = true;
                switch (msgArgs.Message)
                {
                    case DeviceAdded da:
                        Assert.True(da.DeviceName == "TestDevice");
                        Assert.True(da.DeviceIndex == 0);
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
                        Assert.True(dl.Devices[0].DeviceIndex == 0);
                        Assert.True(dl.Devices[0].DeviceName == "TestDevice");
                        break;
                }
                Assert.True(i == 0 ? msgReceived : !msgReceived, "DeviceAdded fired at incorrect time!");
                msgReceived = false;
            }
        }
    }
}