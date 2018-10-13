// <copyright file="ButtplugServerTests.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using Buttplug.Core.Test;
using NUnit.Framework;

namespace Buttplug.Server.Test
{
    [TestFixture]
    public class ButtplugServerTests
    {
        private TestServer _server;

        [SetUp]
        public async Task ServerTestSetup()
        {
            _server = new TestServer();
            Assert.True(await _server.SendMessageAsync(new RequestServerInfo("TestClient")) is ServerInfo);
        }

        [Test]
        public async Task RejectOutgoingOnlyMessage()
        {
            Assert.True(await _server.SendMessageAsync(new Error("Error", Error.ErrorClass.ERROR_UNKNOWN, ButtplugConsts.DefaultMsgId)) is Error);
        }

        [Test]
        public async Task LoggerSettingsTest()
        {
            var gotMessage = false;
            _server.MessageReceived += (aObj, aMsg) =>
            {
                if (aMsg.Message.GetType() == typeof(Log))
                {
                    gotMessage = true;
                }
            };

            // Sending error messages will always cause an error, as they are outgoing, not incoming.
            Assert.True(await _server.SendMessageAsync(new Error("Error", Error.ErrorClass.ERROR_UNKNOWN, ButtplugConsts.DefaultMsgId)) is Error);
            Assert.False(gotMessage);
            Assert.True(await _server.SendMessageAsync(new RequestLog(ButtplugLogLevel.Trace)) is Ok);
            Assert.True(await _server.SendMessageAsync(new Error("Error", Error.ErrorClass.ERROR_UNKNOWN, ButtplugConsts.DefaultMsgId)) is Error);
            Assert.True(gotMessage);
            await _server.SendMessageAsync(new RequestLog(ButtplugLogLevel.Off));
            gotMessage = false;
            await _server.SendMessageAsync(new Error("Error", Error.ErrorClass.ERROR_UNKNOWN, ButtplugConsts.DefaultMsgId));
            Assert.False(gotMessage);
        }

        [Test]
        public async Task CheckMessageReturnId()
        {
            _server.MessageReceived += (aObj, aMsg) =>
            {
                Assert.True(aMsg.Message is RequestServerInfo);
                Assert.True(aMsg.Message.Id == 12345);
            };
            var m = new RequestServerInfo("TestClient", 12345);
            await _server.SendMessageAsync(m);
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

        private async Task CheckDeviceCount(ButtplugServer aServer, int aExpectedCount)
        {
            var deviceListMsg = await aServer.SendMessageAsync(new RequestDeviceList());
            Assert.True(deviceListMsg is DeviceList);
            Assert.AreEqual(((DeviceList)deviceListMsg).Devices.Length, aExpectedCount);
        }

        [Test]
        public async Task TestAddListRemoveDevices()
        {
            var d = new TestDevice(new ButtplugLogManager(), "TestDevice");
            var msgarray = d.AllowedMessageTypes;
            var enumerable = msgarray as Type[] ?? msgarray.ToArray();
            Assert.True(enumerable.Length == 3);
            Assert.True(enumerable.Contains(typeof(SingleMotorVibrateCmd)));
            _server.AddDeviceSubtypeManager(aLogger => new TestDeviceSubtypeManager(d));
            ButtplugMessage msgReceived = null;
            _server.MessageReceived += (aObj, aMsgArgs) =>
            {
                if (!(aMsgArgs.Message is ScanningFinished))
                {
                    msgReceived = aMsgArgs.Message;
                }

                CheckDeviceMessages(msgReceived);
            };
            await CheckDeviceCount(_server, 0);
            Assert.True(await _server.SendMessageAsync(new StartScanning()) is Ok);
            Assert.True(await _server.SendMessageAsync(new StopScanning()) is Ok);
            Assert.True(msgReceived is DeviceAdded);
            await CheckDeviceCount(_server, 1);
            msgReceived = await _server.SendMessageAsync(new RequestDeviceList());
            d.RemoveDevice();
            Assert.True(msgReceived is DeviceRemoved);
            await CheckDeviceCount(_server, 0);
        }

        [Test]
        public async Task TestIncomingSystemIdMessage()
        {
            // Test echos back a test message with the same string and id
            Assert.True(await _server.SendMessageAsync(new Core.Messages.Test("Right", 2)) is Core.Messages.Test);
            Assert.True(await _server.SendMessageAsync(new Core.Messages.Test("Wrong", 0)) is Error);
        }

        [Test]
        public async Task TestInvalidDeviceIdMessage()
        {
            Assert.True(await _server.SendMessageAsync(new SingleMotorVibrateCmd(1, .2, 0)) is Error);
        }

        [Test]
        public async Task TestValidDeviceMessage()
        {
            var d = new TestDevice(new ButtplugLogManager(), "TestDevice");
            var m = new TestDeviceSubtypeManager(d);
            _server.AddDeviceSubtypeManager(aLogger => m);
            Assert.True(await _server.SendMessageAsync(new StartScanning()) is Ok);
            Assert.True(await _server.SendMessageAsync(new StopScanning()) is Ok);
            Assert.True(await _server.SendMessageAsync(new SingleMotorVibrateCmd(1, .2)) is Ok);
        }

        [Test]
        public async Task TestInvalidDeviceMessage()
        {
            var d = new TestDevice(new ButtplugLogManager(), "TestDevice");
            var m = new TestDeviceSubtypeManager(d);
            _server.AddDeviceSubtypeManager(aLogger => m);
            Assert.True(await _server.SendMessageAsync(new StartScanning()) is Ok);
            Assert.True(await _server.SendMessageAsync(new StopScanning()) is Ok);
            Assert.True(await _server.SendMessageAsync(new FleshlightLaunchFW12Cmd(1, 0, 0)) is Error);
        }

        [Test]
        public async Task TestDuplicateDeviceAdded()
        {
            var d = new TestDevice(new ButtplugLogManager(), "TestDevice");
            var m = new TestDeviceSubtypeManager(d);
            _server.AddDeviceSubtypeManager(aLogger => m);
            var msgReceived = false;
            _server.MessageReceived += (aObj, aMsgArgs) =>
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
                Assert.True(await _server.SendMessageAsync(new StartScanning()) is Ok);
                Assert.True(await _server.SendMessageAsync(new StopScanning()) is Ok);
                var x = await _server.SendMessageAsync(new RequestDeviceList());
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
        public async Task TestPing()
        {
            var server = new TestServer(100);
            Assert.True(await server.SendMessageAsync(new RequestServerInfo("TestClient")) is ServerInfo);

            // Timeout is set to 100ms
            for (int i = 0; i < 8; i++)
            {
                Thread.Sleep(50);
                Assert.True(await server.SendMessageAsync(new Ping()) is Ok);
            }

            // If we're still getting OK, we've suvived 400ms

            // Now lets ensure we can actually timeout
            Thread.Sleep(150);
            Assert.True(await server.SendMessageAsync(new Ping()) is Error);
        }

        [Test]
        public async Task TestDeviceListMessageListDowngrade()
        {
            // If we request a DeviceAdded/DeviceList message on a client with an older spec version
            // than the server, it should remove all non-spec-supported message types.
            _server = new TestServer();
            Assert.True(await _server.SendMessageAsync(new RequestServerInfo("TestClient", 1, 0)) is ServerInfo);

            var d = new TestDevice(new ButtplugLogManager(), "TestDevice");
            var m = new TestDeviceSubtypeManager(d);
            _server.AddDeviceSubtypeManager(aLogger => m);
            var msgReceived = false;
            _server.MessageReceived += (aObj, aMsgArgs) =>
            {
                switch (aMsgArgs.Message)
                {
                    case DeviceAdded da:
                        msgReceived = true;
                        Assert.True(da.DeviceMessages.Keys.Contains("StopDeviceCmd"));
                        Assert.True(da.DeviceMessages.Keys.Contains("SingleMotorVibrateCmd"));
                        // Should not contain VibrateCmd, even though it is part of the device otherwise.
                        Assert.False(da.DeviceMessages.Keys.Contains("VibrateCmd"));
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
                Assert.True(await _server.SendMessageAsync(new StartScanning()) is Ok);
                Assert.True(await _server.SendMessageAsync(new StopScanning()) is Ok);
                var x = await _server.SendMessageAsync(new RequestDeviceList());
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
    }
}
