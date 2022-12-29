// <copyright file="ButtplugClientConnectorTestBase.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

// Test file, disable ConfigureAwait checking.
// ReSharper disable ConsiderUsingConfigureAwait

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
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
        protected volatile TaskCompletionSource<object> _resetSource = new TaskCompletionSource<object>();
        protected ButtplugClient _client;
        protected IButtplugClientConnector _connector;

        public abstract void SetUpConnector();

        [SetUp]
        public void SetUp()
        {
            _resetSource = new TaskCompletionSource<object>();
            SetUpConnector();
        }

        [TearDown]
        public void CleanUp()
        {
            _client?.DisconnectAsync().Wait();
        }

        private void SetEvent()
        {
            _resetSource.SetResult(new object());
        }

        private async Task WaitForEvent()
        {
            if (await Task.WhenAny(_resetSource.Task, Task.Delay(1000)) != _resetSource.Task)
            {
                throw new Exception("Task timeout!");
            }

            _resetSource = new TaskCompletionSource<object>();
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
        /*
        [Test]
        public async Task TestDeviceScanning()
        {
            Task SendFunc(ButtplugClientDevice device, ButtplugMessage msg, CancellationToken token) => Task.CompletedTask;
            var testDevice = new ButtplugClientDevice(_client, SendFunc, 1, "Test Device", new Dictionary<string, MessageAttributes>()
            {
                { "SingleMotorVibrateCmd", new MessageAttributes() },
                { "VibrateCmd", new MessageAttributes(2) },
                { "StopDeviceCmd", new MessageAttributes() },
            });

            await _client.ConnectAsync();

            _client.ScanningFinished += (sender, arg) =>
            {
                SetEvent();
            };

            _client.DeviceAdded += (sender, arg) =>
            {
                testDevice.Should().BeEquivalentTo(arg.Device);
                SetEvent();
            };
            await _client.StartScanningAsync();
            await WaitForEvent();
            await _client.StopScanningAsync();
            await WaitForEvent();
        }
        */

        /*
        [Test]
        [Ignore("Doesn't compile under new system, need to figure out device exposure")]
        public async Task TestDeviceMessageRaw()
        {
            await _client.ConnectAsync();
            await _client.StartScanningAsync();
            await _client.StopScanningAsync();
            var device = _client.Devices[0];

            // Test device only takes vibration commands
            device.Awaiting(async device => await device.SendMessageAsync(new FleshlightLaunchFW12Cmd(0, 0, 0))).Should().Throw<ButtplugDeviceException>();

            // Shouldn't throw.
            await device.SendMessageAsync(new SingleMotorVibrateCmd(0, 0.5));

            // todo Expose V1/V2 again
            // device.V1.Should().Be(0.5);
            // device.V2.Should().Be(0.5);
        }

        [Test]
        [Ignore("Doesn't compile under new system, need to figure out device exposure")]
        public async Task TestDeviceMessageHelper()
        {
            await _client.ConnectAsync();
            await _client.StartScanningAsync();
            await _client.StopScanningAsync();
            var device = _client.Devices[0];

            // Test device only takes vibration commands
            device.Awaiting(async device => await device.SendFleshlightLaunchFW12Cmd(0, 0)).Should()
                .Throw<ButtplugDeviceException>();

            // Shouldn't throw.
            await _client.Devices[0].SendVibrateCmd(0.5);

            // TODO expose V1/V2 again
            //_subtypeMgr.Device.V1.Should().Be(0.5);
            //_subtypeMgr.Device.V2.Should().Be(0.5);

            // Shouldn't throw.
            await _client.Devices[0].SendVibrateCmd(new[] { 0.0, 0.5 } );

            //_subtypeMgr.Device.V1.Should().Be(0.0);
            //_subtypeMgr.Device.V2.Should().Be(0.5);
        }
        */

        /*
        [Test]
        public async Task TestDeviceRemovalEvent()
        {
            await _client.ConnectAsync();
            await _client.StartScanningAsync();
            await _client.StopScanningAsync();

            var testDevice = _client.Devices[0];
            _client.DeviceRemoved += (sender, arg) =>
            {
                testDevice.Should().BeEquivalentTo(arg.Device);
                SetEvent();
            };
            _subtypeMgr.Device.Disconnect();
            await WaitForEvent();
            _client.Devices.Length.Should().Be(0);
        }
        */


        [Test]
        public async Task TestSendWithoutConnecting()
        {
            await _client.Awaiting(client => client.StartScanningAsync()).Should().ThrowAsync<ButtplugClientConnectorException>();
        }

        [Test]
        public async Task TestRethrowErrorMessage()
        {
            var c = new SystemMessageSendingClient("TestClient");
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