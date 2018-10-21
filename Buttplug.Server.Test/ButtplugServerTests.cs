// <copyright file="ButtplugServerTests.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

// Test file, disable ConfigureAwait checking.
// ReSharper disable ConsiderUsingConfigureAwait

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using Buttplug.Core.Test;
using FluentAssertions;
using NUnit.Framework;

namespace Buttplug.Server.Test
{
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Test classes can skip documentation requirements")]
    [TestFixture]
    public class ButtplugServerTests
    {
        private TestServer _server;

        [SetUp]
        public async Task ServerTestSetup()
        {
            _server = new TestServer();
            var msg = await _server.SendMessageAsync(new RequestServerInfo("TestClient"));
            msg.Should().BeOfType<ServerInfo>();
        }

        private void SendOutgoingMessageToServer()
        {
            // Sending error messages will always cause an error, as they are outgoing, not incoming.
            _server
                .Awaiting(s =>
                    s.SendMessageAsync(new Error("Error", Error.ErrorClass.ERROR_UNKNOWN, ButtplugConsts.DefaultMsgId)))
                .Should()
                .Throw<ButtplugMessageException>();
        }

        [Test]
        public void TestRejectOutgoingOnlyMessage()
        {
            SendOutgoingMessageToServer();
        }

        [Test]
        public async Task TestLoggerSettings()
        {
            var gotMessage = false;
            _server.MessageReceived += (aObj, aMsg) =>
            {
                if (aMsg.Message.GetType() == typeof(Log))
                {
                    gotMessage = true;
                }
            };

            SendOutgoingMessageToServer();
            gotMessage.Should().BeFalse();
            var msg = await _server.SendMessageAsync(new RequestLog(ButtplugLogLevel.Trace));
            msg.Should().BeOfType<Ok>();
            SendOutgoingMessageToServer();
            gotMessage.Should().BeTrue();
            msg = await _server.SendMessageAsync(new RequestLog());
            msg.Should().BeOfType<Ok>();
            gotMessage = false;
            SendOutgoingMessageToServer();
            gotMessage.Should().BeFalse();
        }

        [Test]
        public async Task CheckMessageReturnId()
        {
            _server.MessageReceived += (aObj, aMsg) =>
            {
                aMsg.Message.Should().BeOfType<Core.Messages.Test>();
                aMsg.Message.Id.Should().Be(12345);
            };
            await _server.SendMessageAsync(new Core.Messages.Test("Test", 12345));
        }

        private void CheckDeviceMessages(ButtplugMessage aMsgArgs)
        {
            switch (aMsgArgs)
            {
                case DeviceAdded da:
                    da.DeviceName.Should().Be("TestDevice");
                    da.DeviceIndex.Should().Be(1);
                    da.DeviceMessages.Count().Should().Be(3);
                    da.DeviceMessages.Should().ContainKey("SingleMotorVibrateCmd");
                    break;
                case DeviceList dl:
                    dl.Devices.Length.Should().Be(1);
                    var di = dl.Devices[0];
                    di.DeviceName.Should().Be("TestDevice");
                    di.DeviceIndex.Should().Be(1);
                    di.DeviceMessages.Count().Should().Be(3);
                    di.DeviceMessages.ContainsKey("SingleMotorVibrateCmd");
                    break;
                case DeviceRemoved dr:
                    dr.DeviceIndex.Should().Be(1);
                    break;
                case ScanningFinished _:
                    break;
                default:
                    Assert.Fail($"Shouldn't be here {aMsgArgs.GetType().Name}");
                    break;
            }
        }

        private async Task CheckDeviceCount(ButtplugServer aServer, int aExpectedCount)
        {
            var deviceListMsg = await aServer.SendMessageAsync(new RequestDeviceList());
            deviceListMsg.Should().BeOfType<DeviceList>();
            ((DeviceList)deviceListMsg).Devices.Length.Should().Be(aExpectedCount);
        }

        [Test]
        public async Task TestAddListRemoveDevices()
        {
            var d = new TestDevice(new ButtplugLogManager(), "TestDevice");
            var msgarray = d.AllowedMessageTypes;
            var enumerable = msgarray as Type[] ?? msgarray.ToArray();
            enumerable.Length.Should().Be(3);
            enumerable.Should().Contain(typeof(SingleMotorVibrateCmd));
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
            (await _server.SendMessageAsync(new StartScanning())).Should().BeOfType<Ok>();
            (await _server.SendMessageAsync(new StopScanning())).Should().BeOfType<Ok>();
            msgReceived.Should().BeOfType<DeviceAdded>();
            await CheckDeviceCount(_server, 1);
            msgReceived = await _server.SendMessageAsync(new RequestDeviceList());
            d.RemoveDevice();
            msgReceived.Should().BeOfType<DeviceRemoved>();
            await CheckDeviceCount(_server, 0);
        }

        [Test]
        public void TestIncomingSystemIdMessage()
        {
            // Test echos back a test message with the same string and id
            _server.Awaiting(s => s.SendMessageAsync(new Core.Messages.Test("Right", 2))).Should().NotThrow();
            _server.Awaiting(s => s.SendMessageAsync(new Core.Messages.Test("Wrong", 0))).Should()
                .Throw<ButtplugMessageException>();
        }

        [Test]
        public void TestInvalidDeviceIdMessage()
        {
            _server.Awaiting(s => s.SendMessageAsync(new SingleMotorVibrateCmd(1, .2))).Should().Throw<ButtplugDeviceException>();
        }

        [Test]
        public async Task TestValidDeviceMessage()
        {
            var d = new TestDevice(new ButtplugLogManager(), "TestDevice");
            var m = new TestDeviceSubtypeManager(d);
            _server.AddDeviceSubtypeManager(aLogger => m);
            (await _server.SendMessageAsync(new StartScanning())).Should().BeOfType<Ok>();
            (await _server.SendMessageAsync(new StopScanning())).Should().BeOfType<Ok>();
            (await _server.SendMessageAsync(new SingleMotorVibrateCmd(1, .2))).Should().BeOfType<Ok>();
        }

        [Test]
        public async Task TestInvalidDeviceMessage()
        {
            var d = new TestDevice(new ButtplugLogManager(), "TestDevice");
            var m = new TestDeviceSubtypeManager(d);
            _server.AddDeviceSubtypeManager(aLogger => m);
            (await _server.SendMessageAsync(new StartScanning())).Should().BeOfType<Ok>();
            (await _server.SendMessageAsync(new StopScanning())).Should().BeOfType<Ok>();
            _server.Awaiting(async aServer => await aServer.SendMessageAsync(new FleshlightLaunchFW12Cmd(1, 0, 0)))
                .Should().Throw<ButtplugDeviceException>();
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
                        da.DeviceName.Should().Be("TestDevice");
                        da.DeviceIndex.Should().Be(1);
                        da.Id.Should().Be(0);
                        break;

                    case ScanningFinished _:
                        break;

                    default:
                        msgReceived = true;
                        aMsgArgs.Message.Should().NotBeOfType<DeviceAdded>();
                        break;
                }
            };
            for (var i = 0; i < 2; ++i)
            {
                (await _server.SendMessageAsync(new StartScanning())).Should().BeOfType<Ok>();
                (await _server.SendMessageAsync(new StopScanning())).Should().BeOfType<Ok>();
                var x = await _server.SendMessageAsync(new RequestDeviceList());
                x.Should().BeOfType<DeviceList>();
                switch (x)
                {
                    case DeviceList dl:
                        dl.Devices.Length.Should().Be(1);
                        dl.Devices[0].DeviceIndex.Should().Be(1U);
                        dl.Devices[0].DeviceName.Should().Be("TestDevice");
                        break;
                }

                (i == 0 ? msgReceived : !msgReceived).Should().BeTrue();
                msgReceived = false;
            }
        }

        [Test]
        public void TestServerLicenseFileLoading()
        {
            var license = ButtplugUtils.GetLicense(Assembly.GetAssembly(typeof(ButtplugServer)), "Buttplug.Server.LICENSE");
            license.Should().Contain("Buttplug is covered under the following BSD 3-Clause License");
            license.Should().Contain("NJsonSchema (https://github.com/RSuter/NJsonSchema) is covered under the");
        }

        [Test]
        public async Task TestPing()
        {
            var server = new TestServer(100);
            var msg = await server.SendMessageAsync(new RequestServerInfo("TestClient"));
            msg.Should().BeOfType<ServerInfo>();

            // Timeout is set to 100ms
            for (int i = 0; i < 8; i++)
            {
                Thread.Sleep(50);
                (await server.SendMessageAsync(new Ping())).Should().BeOfType<Ok>();
            }

            // If we're still getting OK, we've survived 400ms

            // Now lets ensure we can actually timeout
            Thread.Sleep(150);
            server.Awaiting(async aServer => await aServer.SendMessageAsync(new Ping())).Should().Throw<ButtplugPingException>();
        }

        [Test]
        public async Task TestDeviceListMessageListDowngrade()
        {
            // If we request a DeviceAdded/DeviceList message on a client with an older spec version
            // than the server, it should remove all non-spec-supported message types.
            _server = new TestServer();
            (await _server.SendMessageAsync(new RequestServerInfo("TestClient", 1, 0))).Should().BeOfType<ServerInfo>();

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
                        da.DeviceMessages.Keys.Should().Contain("StopDeviceCmd");
                        da.DeviceMessages.Keys.Should().Contain("SingleMotorVibrateCmd");

                        // Should not contain VibrateCmd, even though it is part of the device otherwise.
                        da.DeviceMessages.Keys.Should().NotContain("VibrateCmd");
                        break;

                    case ScanningFinished _:
                        break;

                    default:
                        msgReceived = true;
                        aMsgArgs.Message.Should().NotBeOfType<DeviceAdded>();
                        break;
                }
            };
            for (var i = 0; i < 2; ++i)
            {
                (await _server.SendMessageAsync(new StartScanning())).Should().BeOfType<Ok>();
                (await _server.SendMessageAsync(new StopScanning())).Should().BeOfType<Ok>();
                var x = await _server.SendMessageAsync(new RequestDeviceList());
                x.Should().BeOfType<DeviceList>();
                var dl = x as DeviceList;

                dl.Devices.Length.Should().Be(1);
                dl.Devices[0].DeviceIndex.Should().Be(1U);
                dl.Devices[0].DeviceName.Should().Be("TestDevice");

                (i == 0 ? msgReceived : !msgReceived).Should().BeTrue();
                msgReceived = false;
            }
        }
    }
}
