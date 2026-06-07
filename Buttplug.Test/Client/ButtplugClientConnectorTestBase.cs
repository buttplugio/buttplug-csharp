// <copyright file="ButtplugClientConnectorTestBase.cs" company="Nonpolynomial Labs LLC">
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
    public abstract class ButtplugClientConnectorTestBase
    {
        protected ButtplugClient _client;
        protected IButtplugClientConnector _connector;

        public abstract void SetUpConnector();

        [SetUp]
        public void SetUp()
        {
            SetUpConnector();
        }

        [TearDown]
        public void CleanUp()
        {
            _client?.DisconnectAsync().Wait();
        }

        [Test]
        public async Task TestBasicConnectDisconnect()
        {
            _client.Connected.Should().BeFalse();
            await _client.ConnectAsync(_connector);
            _client.Connected.Should().BeTrue();
            await _client.DisconnectAsync();
            _client.Connected.Should().BeFalse();
        }

        [Test]
        public async Task TestExceptionOnDoubleConnect()
        {
            _client.Connected.Should().BeFalse();
            await _client.ConnectAsync(_connector);
            _client.Connected.Should().BeTrue();
            await _client.Awaiting(client => client.ConnectAsync(_connector))
                .Should().ThrowAsync<ButtplugHandshakeException>();
        }

        [Test]
        public async Task TestSendWithoutConnecting()
        {
            await _client.Awaiting(client => client.StartScanningAsync()).Should().ThrowAsync<ButtplugClientConnectorException>();
        }

        [Test]
        public async Task TestRethrowErrorMessage()
        {
            var c = new SystemMessageSendingClient("TestClient");
            ((ButtplugClientTestConnector)_connector)
                .SetMessageResponse<Ok>(
                    new Error("Cannot send outgoing-only message", Error.ErrorClass.ERROR_MSG, ButtplugConsts.DefaultMsgId));
            await c.ConnectAsync(_connector);

            // For some reason, trying this with FluentAssertions Awaiting clauses causes a stall. Back to Asserts.
            try
            {
                await c.SendOutgoingOnlyMessage();
                Assert.Fail("Should throw!");
            }
            catch (ButtplugMessageException)
            {
                Assert.Pass("Got expected exception");
            }
        }
    }
}
