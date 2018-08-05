using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;
using Buttplug.Server.Test;
using NUnit.Framework;

namespace Buttplug.Client.Test
{
    public abstract class ButtplugClientConnectorTestBase
    {
        protected volatile TaskCompletionSource<object> _resetSource = new TaskCompletionSource<object>();
        protected ButtplugClient _client;
        protected IButtplugClientConnector _connector;
        protected TestDeviceSubtypeManager _subtypeMgr;
        protected ButtplugLogManager _logMgr;

        public abstract void SetUpConnector();

        [SetUp]
        public void SetUp()
        {
            _resetSource = new TaskCompletionSource<object>();
            _logMgr = new ButtplugLogManager();
            SetUpConnector();
        }

        [TearDown]
        public void CleanUp()
        {
            _client?.Disconnect().Wait();
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
            Assert.False(_client.Connected);
            await _client.Connect();
            Assert.True(_client.Connected);
            await _client.Disconnect();
            Assert.False(_client.Connected);
        }

        [Test]
        public async Task TestDeviceScanning()
        {
            var testDevice = new ButtplugClientDevice(1, "Test Device", new Dictionary<string, MessageAttributes>()
            {
                { "SingleMotorVibrateCmd", new MessageAttributes() },
                { "VibrateCmd", new MessageAttributes(2) },
                { "StopDeviceCmd", new MessageAttributes() },
            });

            await _client.Connect();

            _client.ScanningFinished += (aSender, aArg) =>
            {
                SetEvent();
            };

            _client.DeviceAdded += (aSender, aArg) =>
            {
                Assert.AreEqual(testDevice, aArg.Device);
                SetEvent();
            };
            await _client.StartScanning();
            await WaitForEvent();
            await _client.StopScanning();
            await WaitForEvent();
        }

        [Test]
        public async Task TestDeviceMessage()
        {
            await _client.Connect();
            await _client.StartScanning();
            // await _client.StopScanning();
            Assert.ThrowsAsync<ButtplugClientException>(async () => await _client.SendDeviceMessage(_client.Devices[0], new FleshlightLaunchFW12Cmd(0, 0, 0)));
            var testDevice = new ButtplugClientDevice(2, "Test Device 2", new Dictionary<string, MessageAttributes>()
            {
                { "SingleMotorVibrateCmd", new MessageAttributes() },
                { "VibrateCmd", new MessageAttributes(2) },
            });
            Assert.ThrowsAsync<ButtplugClientException>(async () => await _client.SendDeviceMessage(testDevice, new FleshlightLaunchFW12Cmd(0, 0, 0)));
            // Shouldn't throw.
            await _client.SendDeviceMessage(_client.Devices[0], new SingleMotorVibrateCmd(0, 0.5));
            Assert.AreEqual(_subtypeMgr.Device.V1, 0.5);
            Assert.AreEqual(_subtypeMgr.Device.V2, 0.5);
        }

        [Test]
        public async Task TestDeviceRemovalEvent()
        {
            await _client.Connect();
            await _client.StartScanning();
            // await _client.StopScanning();

            var testDevice = _client.Devices[0];
            _client.DeviceRemoved += (aSender, aArg) =>
            {
                Assert.AreEqual(testDevice, aArg.Device);
                SetEvent();
            };
            _subtypeMgr.Device.Disconnect();
            await WaitForEvent();
            Assert.AreEqual(_client.Devices.Length, 0);
        }

        // TODO Add Log Tests

        // TODO Add Ping Timeout Tests

        // TODO Add connector (server) disconnect event test
    }
}