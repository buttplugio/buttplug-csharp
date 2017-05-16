using System;
using Buttplug.Core;
using Buttplug.Messages;
using ButtplugTest.Core;
using LanguageExt;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace ButtplugTest.Messages
{
    public class ButtplugMessageTests
    {
        [Fact(Skip="Logging broken")]
        public async void RequestLogJsonTest()
        {
            var s = new TestService();
            Assert.False((await s.SendMessage("{\"RequestLog\": {\"LogLevel\":\"Trace\",\"Id\":1}}")) is Error);
        }

        [Fact(Skip="Logging broken")]
        public async void RequestLogWrongLevelTest()
        {
            var s = new TestService();
            Assert.True((await s.SendMessage("{\"RequestLog\": {\"LogLevel\":\"NotALevel\",\"Id\":1}}")) is Error);
        }

        [Fact]
        public async void CallStartScanning()
        {
            var dm = new TestDeviceManager();
            var s = new TestService(dm);
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
            var s = new ButtplugService();
            var r = await s.SendMessage(new FakeMessage(1));
            Assert.True(r is Error);
        }

        [Fact]
        public async void SerializeUnhandledMessage()
        {
            var r = ButtplugJsonMessageParser.Serialize(new FakeMessage(1));
            // Even though the message is defined outside the core library, it should at least serialize
            Assert.True(r.IsSome);
            // However it shouldn't be taken by the server.
            var s = new ButtplugService();
            ButtplugMessage e = null;
            await r.IfSomeAsync(async x => e = await s.SendMessage(x));
            Assert.True(e is Error);
        }

        [Fact]
        public async void CallStopScanning()
        {
            var dm = new TestDeviceManager();
            var s = new TestService(dm);
            var r = await s.SendMessage(new StopScanning());
            Assert.True(r is Ok);
            Assert.True(dm.StopScanningCalled);
        }

        [Fact]
        public async void RequestServerInfoTest()
        {
            var s = new ButtplugService();
            var results = new List<ButtplugMessage>
            {
                await s.SendMessage(new RequestServerInfo()),
                await s.SendMessage("{\"RequestServerInfo\":{\"Id\":1}}")
            };
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
    }
}