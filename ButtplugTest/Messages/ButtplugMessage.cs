using Buttplug.Core;
using Buttplug.Messages;
using ButtplugTest.Core;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace ButtplugTest.Messages
{
    public class ButtplugMessageTests
    {
        [Fact]
        public async void RequestLogJsonTest()
        {
            var s = new TestService();
            var res = await s.SendMessage("[{\"RequestLog\": {\"LogLevel\":\"Off\",\"Id\":1}}]");
            Assert.True(res.Length == 1);
            Assert.True(res[0] is Ok);
        }

        [Fact]
        public async void RequestLogWrongLevelTest()
        {
            var s = new TestService();
            var res = await s.SendMessage("[{\"RequestLog\": {\"LogLevel\":\"NotALevel\",\"Id\":1}}]");
            Assert.True(res.Length == 1);
            Assert.True(res[0] is Error);
        }

        [Fact]
        public async void RequestLogWithoutArrayWrapperTest()
        {
            var s = new TestService();
            var res = await s.SendMessage("{\"RequestLog\": {\"LogLevel\":\"Off\",\"Id\":1}}");
            Assert.True(res.Length == 1);
            Assert.True(res[0] is Error);
        }

        [Fact]
        public async void RequestLogTraceLevelTest()
        {
            var s = new TestService();
            s.MessageReceived += s.OnMessageReceived;
            var res = await s.SendMessage("[{\"RequestLog\": {\"LogLevel\":\"Trace\",\"Id\":1}}]");
            Assert.True(res.Length == 1);
            Assert.True(res[0] is Ok);
            res = await s.SendMessage("[{\"Test\": {\"TestString\":\"Echo\",\"Id\":2}}]");
            Assert.True(res.Length == 1);
            Assert.True(res[0] is Test);
            Assert.True(s.outgoingAsync.Count == 3);
        }

        [Fact]
        public async void RequestNothingTest()
        {
            var s = new TestService();
            var res = await s.SendMessage("[]");
            Assert.True(res.Length == 1);
            Assert.True(res[0] is Error);
        }

        [Fact]
        public async void CallStartScanning()
        {
            var dm = new TestDeviceSubtypeManager(new ButtplugLogManager());
            var s = new TestService();
            s.AddDeviceSubtypeManager(dm);
            var r = await s.SendMessage(new StartScanning());
            Assert.True(r is Ok);
            Assert.True(dm.StartScanningCalled);
        }

        public class FakeMessage : ButtplugMessage
        {
            public FakeMessage(uint aId) : base(aId)
            {
            }
        };

        [Fact]
        public async void SendUnhandledMessage()
        {
            var s = new TestService();
            var r = await s.SendMessage(new FakeMessage(1));
            Assert.True(r is Error);
        }

        [Fact]
        public async void SerializeUnhandledMessage()
        {
            var logger = new ButtplugLogManager();
            var r = new ButtplugJsonMessageParser(logger).Serialize(new FakeMessage(1));
            // Even though the message is defined outside the core library, it should at least serialize
            Assert.True(r.Length > 0);
            // However it shouldn't be taken by the server.
            var s = new TestService();
            var e = await s.SendMessage(r);
            Assert.True(e.Length == 1);
            Assert.True(e[0] is Error);
        }

        [Fact]
        public async void CallStopScanning()
        {
            var dm = new TestDeviceSubtypeManager(new ButtplugLogManager());
            var s = new TestService();
            s.AddDeviceSubtypeManager(dm);
            var r = await s.SendMessage(new StopScanning());
            Assert.True(r is Ok);
            Assert.True(dm.StopScanningCalled);
        }

        [Fact]
        public async void RequestServerInfoTest()
        {
            var s = new ButtplugService("TestClient", 100);
            var results = new List<ButtplugMessage> {await s.SendMessage(new RequestServerInfo("TestClient"))};
            results.AddRange(await s.SendMessage("[{\"RequestServerInfo\":{\"Id\":1, \"ClientName\":\"TestClient\"}}]"));

            foreach (var reply in results)
            {
                Assert.True(reply is ServerInfo);
                var r = (ServerInfo)reply;
                if (r is null)
                {
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
            var s = new ButtplugService("TestClient", 100);
            Assert.True(await s.SendMessage(new Test("Test")) is Error);
            Assert.True(await s.SendMessage(new RequestServerInfo("TestClient")) is ServerInfo);
            Assert.True(await s.SendMessage(new Test("Test")) is Test);
        }
    }
}