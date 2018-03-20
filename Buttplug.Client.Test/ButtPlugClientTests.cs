using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Components.WebsocketServer;
using Buttplug.Core;
using Buttplug.Core.Messages;
using Buttplug.Server;
using Buttplug.Server.Test;
using NUnit.Framework;

namespace Buttplug.Client.Test
{
    [TestFixture]
    public class ButtplugClientTests : IButtplugServerFactory
    {
        private class ButtplugTestClient : ButtplugWSClient
        {
            public ButtplugTestClient(string aClientName)
                : base(aClientName)
            {
            }

            public async Task<ButtplugMessage> SendMsg(ButtplugMessage aMsg)
            {
                return await SendMessage(aMsg);
            }
        }

        private ButtplugTestClient _client;
        private ButtplugWebsocketServer _server;
        private TestDeviceSubtypeManager _subtypeMgr;
        private DeviceManager _devMgr;
        private ButtplugLogManager _logMgr;

        public ButtplugServer GetServer()
        {
            return new TestServer(200, _devMgr, false);
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _logMgr = new ButtplugLogManager();
            _devMgr = new DeviceManager(new ButtplugLogManager());
            _subtypeMgr = new TestDeviceSubtypeManager();
            _devMgr.AddDeviceSubtypeManager(_subtypeMgr);
        }

        [TearDown]
        public void CleanUp()
        {
            _client?.Disconnect();
            _server?.Disconnect();
        }

        [Test]
        public void TestConnection()
        {
            var eEvent = new AutoResetEvent(false);

            _subtypeMgr.AddDevice(new TestDevice(_logMgr, "A", "1"));
            _server = new ButtplugWebsocketServer();
            _server.StartServer(this);

            _client = new ButtplugTestClient("Test client");
            _client.Connect(new Uri("ws://localhost:12345/buttplug")).Wait();

            var msgId = _client.NextMsgId;
            var res = _client.SendMsg(new Core.Messages.Test("Test string", msgId)).GetAwaiter().GetResult();
            Assert.True(res != null);
            Assert.True(res is Core.Messages.Test);
            Assert.True(((Core.Messages.Test)res).TestString == "Test string");
            Assert.True(((Core.Messages.Test)res).Id > msgId);

            // Check ping is working
            Thread.Sleep(400);

            msgId = _client.NextMsgId;
            res = _client.SendMsg(new Core.Messages.Test("Test string", msgId)).GetAwaiter().GetResult();
            Assert.True(res != null);
            Assert.True(res is Core.Messages.Test);
            Assert.True(((Core.Messages.Test)res).TestString == "Test string");
            Assert.True(((Core.Messages.Test)res).Id > msgId);

            res = _client.SendMsg(new Core.Messages.Test("Test string")).GetAwaiter().GetResult();
            Assert.True(res != null);
            Assert.True(res is Core.Messages.Test);
            Assert.True(((Core.Messages.Test)res).TestString == "Test string");
            Assert.True(((Core.Messages.Test)res).Id > msgId);

            Assert.True(_client.NextMsgId > 5);

            // Test that events are raised
            var scanningFinished = false;
            ButtplugClientDevice lastAdded = null;
            ButtplugClientDevice lastRemoved = null;
            _client.ScanningFinished += (aSender, aArg) =>
            {
                scanningFinished = true;
                eEvent.Set();
            };

            _client.DeviceAdded += (aSender, aArg) =>
            {
                lastAdded = aArg.Device;
                eEvent.Set();
            };

            _client.DeviceRemoved += (aSender, aArg) =>
            {
                lastRemoved = aArg.Device;
                eEvent.Set();
            };
            _client.StartScanning().Wait();
            Assert.Null(lastAdded);
            _subtypeMgr.AddDevice(new TestDevice(_logMgr, "B", "2"));
            eEvent.WaitOne(10000);
            eEvent.Reset();
            Assert.NotNull(lastAdded);
            Assert.AreEqual("B", lastAdded.Name);

            Assert.True(!scanningFinished);
            _client.StopScanning().Wait();
            eEvent.WaitOne(10000);
            eEvent.Reset();
            Assert.True(scanningFinished);

            Assert.AreEqual(2, _client.Devices.Length);
            Assert.AreEqual("A", _client.Devices[0].Name);
            Assert.AreEqual("B", _client.Devices[1].Name);

            eEvent.Reset();
            Assert.Null(lastRemoved);
            foreach (var dev in _devMgr._devices.Values)
            {
                if ((dev as TestDevice)?.Identifier == "2")
                {
                    (dev as TestDevice).RemoveDevice();
                }
            }

            eEvent.WaitOne(10000);
            eEvent.Reset();
            Assert.NotNull(lastRemoved);
            Assert.AreEqual("B", lastRemoved.Name);
            Assert.AreEqual(1, _client.Devices.Length);
            Assert.AreEqual("A", _client.Devices[0].Name);

            // Shut it down
            _client.Disconnect().Wait();
            _server.StopServer();
        }

        [Test]
        public void TestSSLConnection()
        {
            _server = new ButtplugWebsocketServer();
            _server.StartServer(this, 12346, true, true);

            _client = new ButtplugTestClient("Test client");
            _client.Connect(new Uri("wss://localhost:12346/buttplug"), true).Wait();

            var msgId = _client.NextMsgId;
            var res = _client.SendMsg(new Core.Messages.Test("Test string", msgId)).GetAwaiter().GetResult();
            Assert.True(res != null);
            Assert.True(res is Core.Messages.Test);
            Assert.True(((Core.Messages.Test)res).TestString == "Test string");
            Assert.True(((Core.Messages.Test)res).Id > msgId);

            // Check ping is working
            Thread.Sleep(400);

            msgId = _client.NextMsgId;
            res = _client.SendMsg(new Core.Messages.Test("Test string", msgId)).GetAwaiter().GetResult();
            Assert.True(res != null);
            Assert.True(res is Core.Messages.Test);
            Assert.True(((Core.Messages.Test)res).TestString == "Test string");
            Assert.True(((Core.Messages.Test)res).Id > msgId);

            Assert.True(_client.NextMsgId > 4);

            // Shut it down
            _client.Disconnect().Wait();
            _server.StopServer();
        }
    }
}
