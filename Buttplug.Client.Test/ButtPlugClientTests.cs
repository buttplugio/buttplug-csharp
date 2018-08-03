using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;
using Buttplug.Server;
using Buttplug.Server.Test;
using NUnit.Framework;

namespace Buttplug.Client.Test
{
    [TestFixture]
    public class ButtplugClientTests
    {
        private AutoResetEvent _resetEvent;
        private ButtplugClient _client;
        private ButtplugServer _server;
        private ButtplugEmbeddedConnector _connector;
        private TestDeviceSubtypeManager _subtypeMgr;
        private ButtplugLogManager _logMgr;

        [SetUp]
        public void SetUp()
        {
            _resetEvent = new AutoResetEvent(false);
            _logMgr = new ButtplugLogManager();
            _subtypeMgr = new TestDeviceSubtypeManager(new TestDevice(_logMgr, "Test Device"));
            _server = new TestServer();
            _server.AddDeviceSubtypeManager(_subtypeMgr);
            _connector = new ButtplugEmbeddedConnector(_server, "Test Server", 100);
            _client = new ButtplugClient("Test Client", _connector);
        }

        [TearDown]
        public void CleanUp()
        {
            _client?.Disconnect();
        }

        private void WaitForEvent()
        {
            if (!_resetEvent.WaitOne(100))
            {
                throw new Exception("Event timeout!");
            }

            _resetEvent.Reset();
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
        public void TestClientDeviceEquality()
        {
            var testDevice = new ButtplugClientDevice(1, "Test Device", new Dictionary<string, MessageAttributes>()
            {
                { "SingleMotorVibrateCmd", new MessageAttributes() },
                { "VibrateCmd", new MessageAttributes(2) },
                { "StopDeviceCmd", new MessageAttributes() },
            });
            var testDevice2 = new ButtplugClientDevice(1, "Test Device", new Dictionary<string, MessageAttributes>()
            {
                { "SingleMotorVibrateCmd", new MessageAttributes() },
                { "VibrateCmd", new MessageAttributes(2) },
                { "StopDeviceCmd", new MessageAttributes() },
            });
            var testDevice3 = new ButtplugClientDevice(1, "Test Device", new Dictionary<string, MessageAttributes>()
            {
                { "SingleMotorVibrateCmd", new MessageAttributes() },
                { "VibrateCmd", new MessageAttributes(2) },
            });
            var testDevice4 = new ButtplugClientDevice(1, "Test Device", new Dictionary<string, MessageAttributes>()
            {
                { "SingleMotorVibrateCmd", new MessageAttributes() },
                { "VibrateCmd", new MessageAttributes(2) },
                { "DifferentName", new MessageAttributes() },
            });
            var testDevice5 = new ButtplugClientDevice(1, "Test Device", new Dictionary<string, MessageAttributes>()
            {
                { "SingleMotorVibrateCmd", new MessageAttributes() },
                { "VibrateCmd", new MessageAttributes(2) },
                { "StopDeviceCmd", new MessageAttributes() },
                { "TooMany", new MessageAttributes() },
            });

            Assert.AreEqual(testDevice, testDevice2);
            Assert.AreNotEqual(testDevice, testDevice3);
            Assert.AreNotEqual(testDevice, testDevice4);
            Assert.AreNotEqual(testDevice, testDevice5);
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
                _resetEvent.Set();
            };

            _client.DeviceAdded += (aSender, aArg) =>
            {
                Assert.AreEqual(testDevice, aArg.Device);
                _resetEvent.Set();
            };
            await _client.StartScanning();
            WaitForEvent();
            await _client.StopScanning();
            WaitForEvent();
        }

        [Test]
        public async Task TestDeviceMessage()
        {
            await _client.Connect();
            await _client.StartScanning();
            await _client.StopScanning();
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
            await _client.StopScanning();

            var testDevice = _client.Devices[0];
            _client.DeviceRemoved += (aSender, aArg) =>
            {
                Assert.AreEqual(testDevice, aArg.Device);
                _resetEvent.Set();
            };
            _subtypeMgr.Device.Disconnect();
            WaitForEvent();
            Assert.AreEqual(_client.Devices.Length, 0);
        }

        // TODO Add Log Tests

        // TODO Add Ping Timeout Tests

        // TODO Add connector (server) disconnect event test
    }
}