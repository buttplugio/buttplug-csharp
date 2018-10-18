// <copyright file="ButtplugClientTests.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Runtime.Remoting;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using FluentAssertions;
using NUnit.Framework;

namespace Buttplug.Client.Test
{
    [TestFixture]
    public class ButtplugClientTests
    {
        [Test]
        public void TestClientDeviceEquality()
        {
            var logMgr = new ButtplugLogManager();
            var client = new ButtplugClient("Test Device Client", new ButtplugEmbeddedConnector("Test Device Server"));
            Task SendFunc(ButtplugClientDevice device, ButtplugMessage msg, CancellationToken token) => Task.CompletedTask;
            var testDevice = new ButtplugClientDevice(logMgr, client, SendFunc, 1, "Test Device", new Dictionary<string, MessageAttributes>()
            {
                { "SingleMotorVibrateCmd", new MessageAttributes() },
                { "VibrateCmd", new MessageAttributes(2) },
                { "StopDeviceCmd", new MessageAttributes() },
            });
            var testDevice2 = new ButtplugClientDevice(logMgr, client, SendFunc, 1, "Test Device", new Dictionary<string, MessageAttributes>()
            {
                { "SingleMotorVibrateCmd", new MessageAttributes() },
                { "VibrateCmd", new MessageAttributes(2) },
                { "StopDeviceCmd", new MessageAttributes() },
            });
            var testDevice3 = new ButtplugClientDevice(logMgr, client, SendFunc, 1, "Test Device", new Dictionary<string, MessageAttributes>()
            {
                { "SingleMotorVibrateCmd", new MessageAttributes() },
                { "VibrateCmd", new MessageAttributes(2) },
            });
            var testDevice4 = new ButtplugClientDevice(logMgr, client, SendFunc, 1, "Test Device", new Dictionary<string, MessageAttributes>()
            {
                { "SingleMotorVibrateCmd", new MessageAttributes() },
                { "VibrateCmd", new MessageAttributes(2) },
                { "DifferentName", new MessageAttributes() },
            });
            var testDevice5 = new ButtplugClientDevice(logMgr, client, SendFunc, 1, "Test Device", new Dictionary<string, MessageAttributes>()
            {
                { "SingleMotorVibrateCmd", new MessageAttributes() },
                { "VibrateCmd", new MessageAttributes(2) },
                { "StopDeviceCmd", new MessageAttributes() },
                { "TooMany", new MessageAttributes() },
            });

            var newClient = new ButtplugClient("Other Test Device Client", new ButtplugEmbeddedConnector("Other Test Device Server"));
            var otherTestDevice = new ButtplugClientDevice(logMgr, newClient, SendFunc, 1, "Test Device", new Dictionary<string, MessageAttributes>()
            {
                { "SingleMotorVibrateCmd", new MessageAttributes() },
                { "VibrateCmd", new MessageAttributes(2) },
                { "StopDeviceCmd", new MessageAttributes() },
            });

            Assert.AreEqual(testDevice, testDevice2);
            Assert.AreNotEqual(testDevice, testDevice3);
            Assert.AreNotEqual(testDevice, testDevice4);
            Assert.AreNotEqual(testDevice, testDevice5);
            Assert.AreNotEqual(testDevice, otherTestDevice);
        }

        private class RequestServerInfoErrrorConnector : ButtplugClientTestConnector
        {
            private ButtplugMessage _returnMessage;

            public RequestServerInfoErrrorConnector(ButtplugMessage aReturnMessage)
            {
                _returnMessage = aReturnMessage;
            }

            public override async Task<ButtplugMessage> SendAsync(ButtplugMessage aMsg, CancellationToken aToken = default(CancellationToken))
            {
                if (aMsg is RequestServerInfo)
                {
                    _returnMessage.Id = aMsg.Id;
                    return _returnMessage;
                }
                Assert.Fail("Should never get here.");
                throw new ButtplugClientException("Should never get here", Error.ErrorClass.ERROR_INIT, ButtplugConsts.SystemMsgId);
            }
        }

        [Test]
        public void TestServerSpecOlderThanClientSpec()
        {
            var newClient = new ButtplugClient("Other Test Device Client", new RequestServerInfoErrrorConnector(new ServerInfo("Old Server", 0, 0)));
            newClient.Awaiting(async aClient => await aClient.ConnectAsync()).Should().Throw<ButtplugClientException>().And.ButtplugErrorMessage.ErrorCode.Should().Be(Error.ErrorClass.ERROR_INIT);
            newClient.Connected.Should().BeFalse();
        }

        [Test]
        public void TestErrorReturnOnRSI()
        {
            var newClient = new ButtplugClient("Other Test Device Client", new RequestServerInfoErrrorConnector(new Error("Who even knows", Error.ErrorClass.ERROR_INIT, ButtplugConsts.DefaultMsgId)));
            newClient.Awaiting(async aClient => await aClient.ConnectAsync()).Should().Throw<ButtplugClientException>().And.ButtplugErrorMessage.ErrorCode.Should().Be(Error.ErrorClass.ERROR_INIT);
            newClient.Connected.Should().BeFalse();
        }

        [Test]
        public void TestOtherReturnOnRSI()
        {
            var okMsg = new Ok(ButtplugConsts.DefaultMsgId);
            var newClient = new ButtplugClient("Other Test Device Client", new RequestServerInfoErrrorConnector(okMsg));
            newClient.Awaiting(async aClient => await aClient.ConnectAsync()).Should().Throw<ButtplugClientException>();
        }
    }
}