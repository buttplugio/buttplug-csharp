// <copyright file="ButtplugClientTestClientServerErrors.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

// Test file, disable ConfigureAwait checking.
// ReSharper disable ConsiderUsingConfigureAwait

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;
using FluentAssertions;
using NUnit.Framework;

namespace Buttplug.Client.Test
{
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Test classes can skip documentation requirements")]
    [TestFixture]
    public class ButtplugClientTestClientServerErrors
    {
        private ButtplugClientTestConnector _connector;
        private ButtplugClient _client;
        private bool _errorInvoked;
        private ButtplugException _currentException;

        public void HandleErrorInvoked(object obj, ButtplugExceptionEventArgs ex)
        {
            if (_errorInvoked)
            {
                Assert.Fail("Multiple errors thrown without resets.");
            }

            _errorInvoked = true;
            _currentException = ex.Exception;
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
        public async Task TestServerSpecOlderThanClientSpec()
        {
            _connector.SetMessageResponse<RequestServerInfo>(new ServerInfo("Old Server", 0, 0));
            await _client.Awaiting(client => client.ConnectAsync()).Should().ThrowAsync<ButtplugHandshakeException>();
            _client.Connected.Should().BeFalse();
        }

        [Test]
        public async Task TestErrorReturnOnRequestServerInfo()
        {
            _connector.SetMessageResponse<RequestServerInfo>(new Error("Who even knows", Error.ErrorClass.ERROR_INIT, ButtplugConsts.DefaultMsgId));
            await _client.Awaiting(client => client.ConnectAsync()).Should().ThrowAsync<ButtplugHandshakeException>();
            _client.Connected.Should().BeFalse();
        }

        [Test]
        public async Task TestOtherReturnOnRequestServerInfo()
        {
            _connector.SetMessageResponse<RequestServerInfo>(new Ok(ButtplugConsts.DefaultMsgId));
            await _client.Awaiting(client => client.ConnectAsync()).Should().ThrowAsync<ButtplugHandshakeException>();
        }

        [Test]
        public async Task TestErrorReturnOnDeviceList()
        {
            _connector.SetMessageResponse<RequestDeviceList>(new Error("Who even knows", Error.ErrorClass.ERROR_INIT, ButtplugConsts.DefaultMsgId));
            await _client.Awaiting(client => client.ConnectAsync()).Should().ThrowAsync<ButtplugHandshakeException>();
            _client.Connected.Should().BeFalse();
        }

        [Test]
        public async Task TestOtherReturnOnDeviceList()
        {
            _connector.SetMessageResponse<RequestDeviceList>(new Ok(ButtplugConsts.DefaultMsgId));
            await _client.Awaiting(client => client.ConnectAsync()).Should().ThrowAsync<ButtplugHandshakeException>();
        }

        [Test]
        public async Task TestUnmatchedOkSent()
        {
            await _client.ConnectAsync();
            _connector.SendServerMessage(new Ok(uint.MaxValue));
            _errorInvoked.Should().BeTrue();
        }

        [Test]
        public async Task TestUnmatchedErrorSent()
        {
            await _client.ConnectAsync();
            _connector.SendServerMessage(new Error("This is an error", Error.ErrorClass.ERROR_MSG, uint.MaxValue));
            _errorInvoked.Should().BeTrue();
            _currentException.ButtplugErrorMessage.ErrorCode.Should().Be(Error.ErrorClass.ERROR_MSG);
        }

        [Test]
        public async Task TestUnmatchedDeviceRemovedSent()
        {
            await _client.ConnectAsync();
            _connector.SendServerMessage(new DeviceRemoved(0));
            _errorInvoked.Should().BeTrue();
            _currentException.ButtplugErrorMessage.ErrorCode.Should().Be(Error.ErrorClass.ERROR_DEVICE);
        }

        [Test]
        public async Task TestForcePingTimeout()
        {
            await _client.ConnectAsync();
            _connector.SendServerMessage(new Error("Ping timeout", Error.ErrorClass.ERROR_PING, ButtplugConsts.SystemMsgId));
            _errorInvoked.Should().BeTrue();
            _currentException.ButtplugErrorMessage.ErrorCode.Should().Be(Error.ErrorClass.ERROR_PING);
            _client.Connected.Should().BeFalse();
        }

        [Test]
        public async Task TestPingSendingError()
        {
            _connector.SetMessageResponse<RequestServerInfo>(new ServerInfo("Test Server", ButtplugConsts.CurrentSpecVersion, 50));
            _connector.SetMessageResponse<Ping>(new Error("Ping timeout", Error.ErrorClass.ERROR_PING, ButtplugConsts.SystemMsgId));
            var waitTask = new TaskCompletionSource<bool>();
            var disconnectTask = new TaskCompletionSource<bool>();

            // This test fails often on CI if we using timing/sleeps. Remove failure on multiple
            // errors and set up own handlers.
            _client.ErrorReceived -= HandleErrorInvoked;
            _client.ErrorReceived += (obj, ex) =>
            {
                waitTask.TrySetResult(true);
            };
            _client.ServerDisconnect += (obj, ex) =>
            {
               disconnectTask.TrySetResult(true);
            };
            await _client.ConnectAsync();
            await waitTask.Task;
            await disconnectTask.Task;
        }

        [Test]
        public async Task TestExpectedOkErrorSent()
        {
            _connector.SetMessageResponse<StartScanning>(new Error("Who even knows", Error.ErrorClass.ERROR_DEVICE, ButtplugConsts.DefaultMsgId));
            await _client.ConnectAsync();
            await _client.Awaiting(client => client.StartScanningAsync()).Should().ThrowAsync<ButtplugDeviceException>();
        }

        [Test]
        public async Task TestExpectedOkWrongMessageSent()
        {
            _connector.SetMessageResponse<StartScanning>(new DeviceList(new DeviceMessageInfo[0], ButtplugConsts.DefaultMsgId));
            await _client.ConnectAsync();
            await _client.Awaiting(client => client.StartScanningAsync()).Should().ThrowAsync<ButtplugMessageException>();
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
            _currentException.Should().BeOfType<ButtplugMessageException>();
        }
    }
}