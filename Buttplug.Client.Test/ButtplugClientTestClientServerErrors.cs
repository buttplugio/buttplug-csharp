// <copyright file="ButtplugClientTestClientServerErrors.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;
using FluentAssertions;
using NUnit.Framework;

namespace Buttplug.Client.Test
{
    [TestFixture]
    public class ButtplugClientTestClientServerErrors
    {
        private ButtplugClientTestConnector _connector;
        private ButtplugClient _client;
        private bool _errorInvoked;
        private ButtplugClientException _currentException;

        public void HandleErrorInvoked(object aObj, ButtplugClientException aEx)
        {
            if (_errorInvoked)
            {
                Assert.Fail("Multiple errors thrown without resets.");
            }
            _errorInvoked = true;
            _currentException = aEx;
        }

        [SetUp]
        public void SetUp()
        {
            _connector = new ButtplugClientTestConnector();
            _client = new ButtplugClient("Other Test Device Client", _connector);
            _client.ErrorReceived += HandleErrorInvoked;
            _errorInvoked = false;
        }

        [Test]
        public void TestServerSpecOlderThanClientSpec()
        {
            _connector.SetMessageResponse<RequestServerInfo>(new ServerInfo("Old Server", 0, 0));
            _client.Awaiting(async aClient => await aClient.ConnectAsync()).Should().Throw<ButtplugClientException>().And.ButtplugErrorMessage.ErrorCode.Should().Be(Error.ErrorClass.ERROR_INIT);
            _client.Connected.Should().BeFalse();
        }

        [Test]
        public void TestErrorReturnOnRequestServerInfo()
        {
            _connector.SetMessageResponse<RequestServerInfo>(new Error("Who even knows", Error.ErrorClass.ERROR_INIT, ButtplugConsts.DefaultMsgId));
            _client.Awaiting(async aClient => await aClient.ConnectAsync()).Should().Throw<ButtplugClientException>().And.ButtplugErrorMessage.ErrorCode.Should().Be(Error.ErrorClass.ERROR_INIT);
            _client.Connected.Should().BeFalse();
        }

        [Test]
        public void TestOtherReturnOnRequestServerInfo()
        {
            _connector.SetMessageResponse<RequestServerInfo>(new Ok(ButtplugConsts.DefaultMsgId));
            _client.Awaiting(async aClient => await aClient.ConnectAsync()).Should().Throw<ButtplugClientException>();
        }

        [Test]
        public void TestErrorReturnOnDeviceList()
        {
            _connector.SetMessageResponse<RequestDeviceList>(new Error("Who even knows", Error.ErrorClass.ERROR_INIT, ButtplugConsts.DefaultMsgId));
            _client.Awaiting(async aClient => await aClient.ConnectAsync()).Should().Throw<ButtplugClientException>().And.ButtplugErrorMessage.ErrorCode.Should().Be(Error.ErrorClass.ERROR_INIT);
            _client.Connected.Should().BeFalse();
        }

        [Test]
        public void TestOtherReturnOnDeviceList()
        {
            _connector.SetMessageResponse<RequestDeviceList>(new Ok(ButtplugConsts.DefaultMsgId));
            _client.Awaiting(async aClient => await aClient.ConnectAsync()).Should().Throw<ButtplugClientException>();
        }

        [Test]
        public async Task TestRandomOkSent()
        {
            await _client.ConnectAsync();
            _connector.SendServerMessage(new Ok(uint.MaxValue));
            _errorInvoked.Should().BeTrue();
        }

        [Test]
        public async Task TestRandomErrorSent()
        {
            await _client.ConnectAsync();
            _connector.SendServerMessage(new Error("This is an error", Error.ErrorClass.ERROR_MSG, uint.MaxValue));
            _errorInvoked.Should().BeTrue();
            _currentException.ButtplugErrorMessage.ErrorCode.Should().Be(Error.ErrorClass.ERROR_MSG);
        }

        [Test]
        public async Task TestBadIncomingJSON()
        {
            var jsonConnector = new ButtplugClientTestJSONConnector();
            var client = new ButtplugClient("JSON Test", jsonConnector);
            client.ErrorReceived += HandleErrorInvoked;
            await client.ConnectAsync();
            jsonConnector.SendServerMessage("This is not json.");
            _errorInvoked.Should().BeTrue();
            _currentException.ButtplugErrorMessage.ErrorCode.Should().Be(Error.ErrorClass.ERROR_MSG);
            _currentException.InnerException.Should().BeOfType<ButtplugParserException>();
        }
    }
}