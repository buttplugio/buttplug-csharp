using System;
using System.Linq;
using System.Threading;
using Buttplug.Core;
using Buttplug.Core.Messages;
using Buttplug.Server;
using Xunit;

namespace Buttplug.Server.Test
{
    public class ButtplugServerTests
    {
        [Fact]
        public async void RejectOutgoingOnlyMessage()
        {
            Assert.True((await new TestService().SendMessage(new Error("Error", Error.ErrorClass.ERROR_UNKNOWN, ButtplugConsts.DefaultMsgId))) is Error);
        }

        [Fact]
        public async void LoggerSettingsTest()
        {
            var gotMessage = false;
            var s = new TestService();
            s.MessageReceived += (aObj, aMsg) =>
            {
                if (aMsg.Message.GetType() == typeof(Log))
                {
                    gotMessage = true;
                }
            };

            // Sending error messages will always cause an error, as they are outgoing, not incoming.
            Assert.True(await s.SendMessage(new Error("Error", Error.ErrorClass.ERROR_UNKNOWN, ButtplugConsts.DefaultMsgId)) is Error);
            Assert.False(gotMessage);
            Assert.True(await s.SendMessage(new RequestLog("Trace")) is Ok);
            Assert.True(await s.SendMessage(new Error("Error", Error.ErrorClass.ERROR_UNKNOWN, ButtplugConsts.DefaultMsgId)) is Error);
            Assert.True(gotMessage);
            await s.SendMessage(new RequestLog("Off"));
            gotMessage = false;
            await s.SendMessage(new Error("Error", Error.ErrorClass.ERROR_UNKNOWN, ButtplugConsts.DefaultMsgId));
            Assert.False(gotMessage);
        }

        [Fact]
        public async void CheckMessageReturnId()
        {
            var s = new TestService();
            s.MessageReceived += (aObj, aMsg) =>
            {
                Assert.True(aMsg.Message is RequestServerInfo);
                Assert.True(aMsg.Message.Id == 12345);
            };
            var m = new RequestServerInfo("TestClient", 12345);
            await s.SendMessage(m);
            await s.SendMessage("{\"RequestServerInfo\":{\"Id\":12345}}");
        }

        private void CheckDeviceMessages(ButtplugMessage aMsgArgs)
        {
            switch (aMsgArgs)
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
                case ScanningFinished _:
                    break;
                default:
                    Assert.True(false, $"Shouldn't be here {aMsgArgs.GetType().Name}");
                    break;
            }
        }

        private async void CheckDeviceCount(ButtplugService aService, int aExpectedCount)
        {
            var deviceListMsg = await aService.SendMessage(new RequestDeviceList());
            Assert.True(deviceListMsg is DeviceList);
            Assert.Equal(((DeviceList)deviceListMsg).Devices.Length, aExpectedCount);
        }

        [Fact]
        public async void TestAddListRemoveDevices()
        {
            var d = new TestDevice(new ButtplugLogManager(), "TestDevice");
            var msgarray = d.GetAllowedMessageTypes();
            var enumerable = msgarray as Type[] ?? msgarray.ToArray();
            Assert.True(enumerable.Length == 1);
            Assert.True(enumerable.Contains(typeof(SingleMotorVibrateCmd)));
            var s = new TestService();
            s.AddDeviceSubtypeManager(aLogger => new TestDeviceSubtypeManager(new ButtplugLogManager(), d));
            ButtplugMessage msgReceived = null;
            s.MessageReceived += (aObj, aMsgArgs) =>
            {
                if (!(aMsgArgs.Message is ScanningFinished))
                {
                    msgReceived = aMsgArgs.Message;
                }

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
            Assert.True(await s.SendMessage(new Core.Messages.Test("Right", 2)) is Core.Messages.Test);
            Assert.True(await s.SendMessage(new Core.Messages.Test("Wrong", 0)) is Error);
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
            s.AddDeviceSubtypeManager(aLogger => { return m; });
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
            s.AddDeviceSubtypeManager(aLogger => { return m; });
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
            s.AddDeviceSubtypeManager(aLogger => { return m; });
            var msgReceived = false;
            s.MessageReceived += (aObj, aMsgArgs) =>
            {
                switch (aMsgArgs.Message)
                {
                    case DeviceAdded da:
                        msgReceived = true;
                        Assert.True(da.DeviceName == "TestDevice");
                        Assert.True(da.DeviceIndex == 1);
                        Assert.True(da.Id == 0);
                        break;

                    case ScanningFinished _:
                        break;

                    default:
                        msgReceived = true;
                        Assert.False(aMsgArgs.Message is DeviceAdded);
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
                        Assert.True(dl.Devices.Length == 1);
                        Assert.True(dl.Devices[0].DeviceIndex == 1);
                        Assert.True(dl.Devices[0].DeviceName == "TestDevice");
                        break;
                }

                Assert.True(i == 0 ? msgReceived : !msgReceived, "DeviceAdded fired at incorrect time!");
                msgReceived = false;
            }
        }

        [Fact]
        public void TestLicenseFileLoading()
        {
            var license = ButtplugService.GetLicense();
            Assert.Contains("Buttplug is covered under the following BSD 3-Clause License", license);
            Assert.Contains("NJsonSchema (https://github.com/RSuter/NJsonSchema) is covered under the", license);
        }

        [Fact]
        public async void TestPing()
        {
            var s = new TestService(100);

            // Timeout is set to 100ms
            for (int i = 0; i < 8; i++)
            {
                Thread.Sleep(50);
                Assert.True(await s.SendMessage(new Ping()) is Ok);
            }

            // If we're still getting OK, we've suvived 400ms

            // Now lets ensure we can actually timeout
            Thread.Sleep(150);
            Assert.True(await s.SendMessage(new Ping()) is Error);
        }
    }
}
