using System;
using System.Collections.Generic;
using System.Reflection;
using Buttplug.Core;
using Buttplug.Core.Messages;
using NUnit.Framework;

namespace Buttplug.Server.Test
{
    [TestFixture]
    public class ButtplugMessageTests
    {
        [Test]
        public void RequestLogJsonTest()
        {
            var s = new TestServer();
            var res = s.SendMessage("[{\"RequestLog\": {\"LogLevel\":\"Off\",\"Id\":1}}]").GetAwaiter().GetResult();
            Assert.True(res.Length == 1);
            Assert.True(res[0] is Ok);
        }

        [Test]
        public void RequestLogWrongLevelTest()
        {
            var s = new TestServer();
            var res = s.SendMessage("[{\"RequestLog\": {\"LogLevel\":\"NotALevel\",\"Id\":1}}]").GetAwaiter().GetResult();
            Assert.True(res.Length == 1);
            Assert.True(res[0] is Error);
        }

        [Test]
        public void RequestLogWithoutArrayWrapperTest()
        {
            var s = new TestServer();
            var res = s.SendMessage("{\"RequestLog\": {\"LogLevel\":\"Off\",\"Id\":1}}").GetAwaiter().GetResult();
            Assert.True(res.Length == 1);
            Assert.True(res[0] is Error);
        }

        [Test]
        public void RequestLogTraceLevelTest()
        {
            var s = new TestServer();
            s.MessageReceived += s.OnMessageReceived;
            var res = s.SendMessage("[{\"RequestLog\": {\"LogLevel\":\"Trace\",\"Id\":1}}]").GetAwaiter().GetResult();
            Assert.True(res.Length == 1);
            Assert.True(res[0] is Ok);
            res = s.SendMessage("[{\"Test\": {\"TestString\":\"Echo\",\"Id\":2}}]").GetAwaiter().GetResult();
            Assert.True(res.Length == 1);
            Assert.True(res[0] is Core.Messages.Test);
            Assert.True(s.OutgoingAsync.Count == 3);
        }

        [Test]
        public void RequestNothingTest()
        {
            var s = new TestServer();
            var res = s.SendMessage("[]").GetAwaiter().GetResult();
            Assert.True(res.Length == 1);
            Assert.True(res[0] is Error);
        }

        [Test]
        public void CallStartScanning()
        {
            var dm = new TestDeviceSubtypeManager();
            var s = new TestServer();
            s.AddDeviceSubtypeManager(aLogger => { return dm; });
            var r = s.SendMessage(new StartScanning()).GetAwaiter().GetResult();
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

        [Test]
        public void SendUnhandledMessage()
        {
            var s = new TestServer();
            var r = s.SendMessage(new FakeMessage(1)).GetAwaiter().GetResult();
            Assert.True(r is Error);
        }

        [Test]
        public void SerializeUnhandledMessage()
        {
            var logger = new ButtplugLogManager();
            var r = new ButtplugJsonMessageParser(logger).Serialize(new FakeMessage(1), 0);

            // Even though the message is defined outside the core library, it should at least serialize
            Assert.True(r.Length > 0);

            // However it shouldn't be taken by the server.
            var s = new TestServer();
            var e = s.SendMessage(r).GetAwaiter().GetResult();
            Assert.True(e.Length == 1);
            Assert.True(e[0] is Error);
        }

        [Test]
        public void CallStopScanning()
        {
            var dm = new TestDeviceSubtypeManager();
            var s = new TestServer();
            s.AddDeviceSubtypeManager(aLogger => { return dm; });
            var r = s.SendMessage(new StopScanning()).GetAwaiter().GetResult();
            Assert.True(r is Ok);
            Assert.True(dm.StopScanningCalled);
        }

        [Test]
        public void RequestServerInfoTest()
        {
            var s = new ButtplugServer("TestClient", 100);
            var results = new List<ButtplugMessage> { s.SendMessage(new RequestServerInfo("TestClient")).GetAwaiter().GetResult() };
            results.AddRange(s.SendMessage("[{\"RequestServerInfo\":{\"Id\":1, \"ClientName\":\"TestClient\"}}]").GetAwaiter().GetResult());

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

        [Test]
        public void NonRequestServerInfoFirstTest()
        {
            var s = new ButtplugServer("TestClient", 100);
            Assert.True(s.SendMessage(new Core.Messages.Test("Test")).GetAwaiter().GetResult() is Error);
            Assert.True(s.SendMessage(new RequestServerInfo("TestClient")).GetAwaiter().GetResult() is ServerInfo);
            Assert.True(s.SendMessage(new Core.Messages.Test("Test")).GetAwaiter().GetResult() is Core.Messages.Test);
        }
    }
}
