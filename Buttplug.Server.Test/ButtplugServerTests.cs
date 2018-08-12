// <copyright file="ButtplugServerTests.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Linq;
using System.Threading;
using Buttplug.Core;
using Buttplug.Core.Messages;
using NUnit.Framework;

namespace Buttplug.Server.Test
{
    [TestFixture]
    public class ButtplugServerTests
    {
        [Test]
        public void RejectOutgoingOnlyMessage()
        {
            Assert.True(new TestServer().SendMessage(new Error("Error", Error.ErrorClass.ERROR_UNKNOWN, ButtplugConsts.DefaultMsgId)).GetAwaiter().GetResult() is Error);
        }

        [Test]
        public void LoggerSettingsTest()
        {
            var gotMessage = false;
            var s = new TestServer();
            s.MessageReceived += (aObj, aMsg) =>
            {
                if (aMsg.Message.GetType() == typeof(Log))
                {
                    gotMessage = true;
                }
            };

            // Sending error messages will always cause an error, as they are outgoing, not incoming.
            Assert.True(s.SendMessage(new Error("Error", Error.ErrorClass.ERROR_UNKNOWN, ButtplugConsts.DefaultMsgId)).GetAwaiter().GetResult() is Error);
            Assert.False(gotMessage);
            Assert.True(s.SendMessage(new RequestLog("Trace")).GetAwaiter().GetResult() is Ok);
            Assert.True(s.SendMessage(new Error("Error", Error.ErrorClass.ERROR_UNKNOWN, ButtplugConsts.DefaultMsgId)).GetAwaiter().GetResult() is Error);
            Assert.True(gotMessage);
            s.SendMessage(new RequestLog("Off")).Wait();
            gotMessage = false;
            s.SendMessage(new Error("Error", Error.ErrorClass.ERROR_UNKNOWN, ButtplugConsts.DefaultMsgId)).Wait();
            Assert.False(gotMessage);
        }

        [Test]
        public void CheckMessageReturnId()
        {
            var s = new TestServer();
            s.MessageReceived += (aObj, aMsg) =>
            {
                Assert.True(aMsg.Message is RequestServerInfo);
                Assert.True(aMsg.Message.Id == 12345);
            };
            var m = new RequestServerInfo("TestClient", 12345);
            s.SendMessage(m).Wait();
            s.SendMessage("{\"RequestServerInfo\":{\"Id\":12345}}").Wait();
        }

        private void CheckDeviceMessages(ButtplugMessage aMsgArgs)
        {
            switch (aMsgArgs)
            {
                case DeviceAdded da:
                    Assert.True(da.DeviceName == "TestDevice");
                    Assert.True(da.DeviceIndex == 1);
                    Assert.True(da.DeviceMessages.Count() == 3);
                    Assert.True(da.DeviceMessages.ContainsKey("SingleMotorVibrateCmd"));
                    break;
                case DeviceList dl:
                    Assert.True(dl.Devices.Length == 1);
                    var di = dl.Devices[0];
                    Assert.True(di.DeviceName == "TestDevice");
                    Assert.True(di.DeviceIndex == 1);
                    Assert.True(di.DeviceMessages.Count() == 3);
                    Assert.True(di.DeviceMessages.ContainsKey("SingleMotorVibrateCmd"));
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

        private void CheckDeviceCount(ButtplugServer aServer, int aExpectedCount)
        {
            var deviceListMsg = aServer.SendMessage(new RequestDeviceList()).GetAwaiter().GetResult();
            Assert.True(deviceListMsg is DeviceList);
            Assert.AreEqual(((DeviceList)deviceListMsg).Devices.Length, aExpectedCount);
        }

        [Test]
        public void TestAddListRemoveDevices()
        {
            var d = new TestDevice(new ButtplugLogManager(), "TestDevice");
            var msgarray = d.GetAllowedMessageTypes();
            var enumerable = msgarray as Type[] ?? msgarray.ToArray();
            Assert.True(enumerable.Length == 3);
            Assert.True(enumerable.Contains(typeof(SingleMotorVibrateCmd)));
            var s = new TestServer();
            s.AddDeviceSubtypeManager(aLogger => new TestDeviceSubtypeManager(d));
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
            Assert.True(s.SendMessage(new StartScanning()).GetAwaiter().GetResult() is Ok);
            Assert.True(s.SendMessage(new StopScanning()).GetAwaiter().GetResult() is Ok);
            Assert.True(msgReceived is DeviceAdded);
            CheckDeviceCount(s, 1);
            msgReceived = s.SendMessage(new RequestDeviceList()).GetAwaiter().GetResult();
            d.RemoveDevice();
            Assert.True(msgReceived is DeviceRemoved);
            CheckDeviceCount(s, 0);
        }

        [Test]
        public void TestIncomingSystemIdMessage()
        {
            var s = new TestServer();

            // Test echos back a test message with the same string and id
            Assert.True(s.SendMessage(new Core.Messages.Test("Right", 2)).GetAwaiter().GetResult() is Core.Messages.Test);
            Assert.True(s.SendMessage(new Core.Messages.Test("Wrong", 0)).GetAwaiter().GetResult() is Error);
        }

        [Test]
        public void TestInvalidDeviceIdMessage()
        {
            var s = new TestServer();
            Assert.True(s.SendMessage(new SingleMotorVibrateCmd(1, .2, 0)).GetAwaiter().GetResult() is Error);
        }

        [Test]
        public void TestValidDeviceMessage()
        {
            var d = new TestDevice(new ButtplugLogManager(), "TestDevice");
            var m = new TestDeviceSubtypeManager(d);
            var s = new TestServer();
            s.AddDeviceSubtypeManager(aLogger => { return m; });
            Assert.True(s.SendMessage(new StartScanning()).GetAwaiter().GetResult() is Ok);
            Assert.True(s.SendMessage(new StopScanning()).GetAwaiter().GetResult() is Ok);
            Assert.True(s.SendMessage(new SingleMotorVibrateCmd(1, .2)).GetAwaiter().GetResult() is Ok);
        }

        [Test]
        public void TestInvalidDeviceMessage()
        {
            var d = new TestDevice(new ButtplugLogManager(), "TestDevice");
            var m = new TestDeviceSubtypeManager(d);
            var s = new TestServer();
            s.AddDeviceSubtypeManager(aLogger => { return m; });
            Assert.True(s.SendMessage(new StartScanning()).GetAwaiter().GetResult() is Ok);
            Assert.True(s.SendMessage(new StopScanning()).GetAwaiter().GetResult() is Ok);
            Assert.True(s.SendMessage(new FleshlightLaunchFW12Cmd(1, 0, 0)).GetAwaiter().GetResult() is Error);
        }

        [Test]
        public void TestDuplicateDeviceAdded()
        {
            var d = new TestDevice(new ButtplugLogManager(), "TestDevice");
            var m = new TestDeviceSubtypeManager(d);
            var s = new TestServer();
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
                Assert.True(s.SendMessage(new StartScanning()).GetAwaiter().GetResult() is Ok);
                Assert.True(s.SendMessage(new StopScanning()).GetAwaiter().GetResult() is Ok);
                var x = s.SendMessage(new RequestDeviceList()).GetAwaiter().GetResult();
                Assert.True(x is DeviceList);
                switch (x)
                {
                    case DeviceList dl:
                        Assert.AreEqual(1, dl.Devices.Length);
                        Assert.AreEqual(1U, dl.Devices[0].DeviceIndex);
                        Assert.AreEqual("TestDevice", dl.Devices[0].DeviceName);
                        break;
                }

                Assert.True(i == 0 ? msgReceived : !msgReceived, "DeviceAdded fired at incorrect time!");
                msgReceived = false;
            }
        }

        [Test]
        public void TestLicenseFileLoading()
        {
            var license = ButtplugServer.GetLicense();
            Assert.True(license.Contains("Buttplug is covered under the following BSD 3-Clause License"));
            Assert.True(license.Contains("NJsonSchema (https://github.com/RSuter/NJsonSchema) is covered under the"));
        }

        [Test]
        public void TestPing()
        {
            var s = new TestServer(100);

            // Timeout is set to 100ms
            for (int i = 0; i < 8; i++)
            {
                Thread.Sleep(50);
                Assert.True(s.SendMessage(new Ping()).GetAwaiter().GetResult() is Ok);
            }

            // If we're still getting OK, we've suvived 400ms

            // Now lets ensure we can actually timeout
            Thread.Sleep(150);
            Assert.True(s.SendMessage(new Ping()).GetAwaiter().GetResult() is Error);
        }
    }
}
