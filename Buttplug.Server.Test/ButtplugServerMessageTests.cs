// <copyright file="ButtplugMessageTests.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;
using NUnit.Framework;

namespace Buttplug.Server.Test
{
    [TestFixture]
    public class ButtplugServerMessageTests
    {
        private TestServer _server;

        [SetUp]
        public async Task ServerTestSetup()
        {
            _server = new TestServer();
            Assert.True(await _server.SendMessage(new RequestServerInfo("TestClient")) is ServerInfo);
        }

        [Test]
        public async Task RequestLogJsonTest()
        {
            var res = await _server.SendMessage("[{\"RequestLog\": {\"LogLevel\":\"Off\",\"Id\":1}}]");
            Assert.True(res.Length == 1);
            Assert.True(res[0] is Ok);
        }

        [Test]
        public async Task RequestLogWrongLevelTest()
        {
            var res = await _server.SendMessage("[{\"RequestLog\": {\"LogLevel\":\"NotALevel\",\"Id\":1}}]");
            Assert.True(res.Length == 1);
            Assert.True(res[0] is Error);
        }

        [Test]
        public async Task RequestLogWithoutArrayWrapperTest()
        {
            var res = await _server.SendMessage("{\"RequestLog\": {\"LogLevel\":\"Off\",\"Id\":1}}");
            Assert.True(res.Length == 1);
            Assert.True(res[0] is Error);
        }

        [Test]
        public async Task RequestLogTraceLevelTest()
        {
            _server.MessageReceived += _server.OnMessageReceived;
            var res = await _server.SendMessage("[{\"RequestLog\": {\"LogLevel\":\"Trace\",\"Id\":1}}]");
            Assert.True(res.Length == 1);
            Assert.True(res[0] is Ok);
            res = await _server.SendMessage("[{\"Test\": {\"TestString\":\"Echo\",\"Id\":2}}]");
            Assert.True(res.Length == 1);
            Assert.True(res[0] is Core.Messages.Test);
            Assert.True(_server.OutgoingAsync.Count == 3);
        }

        [Test]
        public async Task RequestNothingTest()
        {
            var res = await _server.SendMessage("[]");
            Assert.True(res[0] is Error);
        }

        [Test]
        public async Task CallStartScanning()
        {
            var dm = new TestDeviceSubtypeManager();
            _server.AddDeviceSubtypeManager(aLogger => { return dm; });
            var r = await _server.SendMessage(new StartScanning());
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
        public async Task SendUnhandledMessage()
        {
            var r = await _server.SendMessage(new FakeMessage(1));
            Assert.True(r is Error);
        }

        [Test]
        public async Task SerializeUnhandledMessage()
        {
            var logger = new ButtplugLogManager();
            var r = new ButtplugJsonMessageParser(logger).Serialize(new FakeMessage(1), 0);

            // Even though the message is defined outside the core library, it should at least serialize
            Assert.True(r.Length > 0);

            // However it shouldn't be taken by the server.
            var e = await _server.SendMessage(r);
            Assert.True(e.Length == 1);
            Assert.True(e[0] is Error);
        }

        [Test]
        public async Task CallStopScanning()
        {
            var dm = new TestDeviceSubtypeManager();
            _server.AddDeviceSubtypeManager(aLogger => dm);
            var r = await _server.SendMessage(new StopScanning());
            Assert.True(r is Ok);
            Assert.True(dm.StopScanningCalled);
        }

        [Test]
        public async Task RequestServerInfoTest()
        {
            var s = new ButtplugServer("TestServer", 100);
            var results = new List<ButtplugMessage> { await s.SendMessage(new RequestServerInfo("TestClient")) };

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
        public async Task NonRequestServerInfoFirstTest()
        {
            var s = new ButtplugServer("TestServer", 0);
            Assert.True(await s.SendMessage(new Core.Messages.Test("Test")) is Error);
            Assert.True(await s.SendMessage(new RequestServerInfo("TestClient")) is ServerInfo);
            Assert.True(await s.SendMessage(new Core.Messages.Test("Test")) is Core.Messages.Test);
        }
    }
}
