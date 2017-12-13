using System;
using System.Collections.Generic;
using System.Reflection;
using Buttplug.Core;
using Buttplug.Core.Messages;
using Buttplug.Server.Test;
using Xunit;

namespace Buttplug.Server.Test
{
    public class ButtplugMessageTests
    {
        [Fact]
        public async void RequestLogJsonTest()
        {
            var s = new TestServer();
            var res = await s.SendMessage("[{\"RequestLog\": {\"LogLevel\":\"Off\",\"Id\":1}}]");
            Assert.True(res.Length == 1);
            Assert.True(res[0] is Ok);
        }

        [Fact]
        public async void RequestLogWrongLevelTest()
        {
            var s = new TestServer();
            var res = await s.SendMessage("[{\"RequestLog\": {\"LogLevel\":\"NotALevel\",\"Id\":1}}]");
            Assert.True(res.Length == 1);
            Assert.True(res[0] is Error);
        }

        [Fact]
        public async void RequestLogWithoutArrayWrapperTest()
        {
            var s = new TestServer();
            var res = await s.SendMessage("{\"RequestLog\": {\"LogLevel\":\"Off\",\"Id\":1}}");
            Assert.True(res.Length == 1);
            Assert.True(res[0] is Error);
        }

        [Fact]
        public async void RequestLogTraceLevelTest()
        {
            var s = new TestServer();
            s.MessageReceived += s.OnMessageReceived;
            var res = await s.SendMessage("[{\"RequestLog\": {\"LogLevel\":\"Trace\",\"Id\":1}}]");
            Assert.True(res.Length == 1);
            Assert.True(res[0] is Ok);
            res = await s.SendMessage("[{\"Test\": {\"TestString\":\"Echo\",\"Id\":2}}]");
            Assert.True(res.Length == 1);
            Assert.True(res[0] is Core.Messages.Test);
            Assert.True(s.OutgoingAsync.Count == 3);
        }

        [Fact]
        public async void RequestNothingTest()
        {
            var s = new TestServer();
            var res = await s.SendMessage("[]");
            Assert.True(res.Length == 1);
            Assert.True(res[0] is Error);
        }

        [Fact]
        public async void CallStartScanning()
        {
            var dm = new TestDeviceSubtypeManager(new ButtplugLogManager());
            var s = new TestServer();
            s.AddDeviceSubtypeManager(aLogger => { return dm; });
            var r = await s.SendMessage(new StartScanning());
            Assert.True(r is Ok);
            Assert.True(dm.StartScanningCalled);
        }

        private class FakeMessage : ButtplugMessage
        {
            public FakeMessage(uint aId)
                : base(aId)
            {
            }
        }

        [Fact]
        public async void SendUnhandledMessage()
        {
            var s = new TestServer();
            var r = await s.SendMessage(new FakeMessage(1));
            Assert.True(r is Error);
        }

        [Fact]
        public async void SerializeUnhandledMessage()
        {
            var logger = new ButtplugLogManager();
            var r = new ButtplugJsonMessageParser(logger).Serialize(new FakeMessage(1), 0);

            // Even though the message is defined outside the core library, it should at least serialize
            Assert.True(r.Length > 0);

            // However it shouldn't be taken by the server.
            var s = new TestServer();
            var e = await s.SendMessage(r);
            Assert.True(e.Length == 1);
            Assert.True(e[0] is Error);
        }

        [Fact]
        public async void CallStopScanning()
        {
            var dm = new TestDeviceSubtypeManager(new ButtplugLogManager());
            var s = new TestServer();
            s.AddDeviceSubtypeManager(aLogger => { return dm; });
            var r = await s.SendMessage(new StopScanning());
            Assert.True(r is Ok);
            Assert.True(dm.StopScanningCalled);
        }

        [Fact]
        public async void RequestServerInfoTest()
        {
            var s = new ButtplugServer("TestClient", 100);
            var results = new List<ButtplugMessage> { await s.SendMessage(new RequestServerInfo("TestClient")) };
            results.AddRange(await s.SendMessage("[{\"RequestServerInfo\":{\"Id\":1, \"ClientName\":\"TestClient\"}}]"));

            foreach (var reply in results)
            {
                ServerInfo r;
                try
                {
                    r = (ServerInfo)reply;
                }
                catch (InvalidCastException)
                {
                    Assert.True(reply is ServerInfo);
                    continue;
                }

                Assert.True(r.MajorVersion == Assembly.GetAssembly(typeof(ServerInfo)).GetName().Version.Major);
                Assert.True(r.MinorVersion == Assembly.GetAssembly(typeof(ServerInfo)).GetName().Version.Minor);
                Assert.True(r.BuildVersion == Assembly.GetAssembly(typeof(ServerInfo)).GetName().Version.Build);
            }
        }

        [Fact]
        public async void NonRequestServerInfoFirstTest()
        {
            var s = new ButtplugServer("TestClient", 100);
            Assert.True(await s.SendMessage(new Core.Messages.Test("Test")) is Error);
            Assert.True(await s.SendMessage(new RequestServerInfo("TestClient")) is ServerInfo);
            Assert.True(await s.SendMessage(new Core.Messages.Test("Test")) is Core.Messages.Test);
        }
    }
}
