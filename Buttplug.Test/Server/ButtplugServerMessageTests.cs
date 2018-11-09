// <copyright file="ButtplugMessageTests.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

// Test file, disable ConfigureAwait checking.
// ReSharper disable ConsiderUsingConfigureAwait

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using FluentAssertions;
using NUnit.Framework;

namespace Buttplug.Server.Test
{
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Test classes can skip documentation requirements")]
    [TestFixture]
    public class ButtplugServerMessageTests
    {
        private TestServer _server;

        [SetUp]
        public async Task TestServer()
        {
            _server = new TestServer();
            var msg = await _server.SendMessageAsync(new RequestServerInfo("TestClient"));
            msg.Should().BeOfType<ServerInfo>();
        }

        [Test]
        public void TestRepeatedHandshake()
        {
            // Sending RequestServerInfo twice should throw, otherwise weird things like Spec version changes could happen.
            _server.Awaiting(async aServer => await aServer.SendMessageAsync(new RequestServerInfo("TestClient")))
                .Should().Throw<ButtplugHandshakeException>();
        }

        [Test]
        public async Task TestRequestLog()
        {
            var res = await _server.SendMessageAsync(new RequestLog(ButtplugLogLevel.Debug));
            res.Should().BeOfType<Ok>();
        }

        [Test]
        public async Task TestStartScanning()
        {
            // Throw if we try to scan with no SubtypeManagers
            // _server.Awaiting(async s => s.SendMessageAsync(new StartScanning())).Should()
            //    .Throw<ButtplugDeviceException>();
            var dm = new TestDeviceSubtypeManager();
            _server.AddDeviceSubtypeManager(aLogger => dm);
            var r = await _server.SendMessageAsync(new StartScanning());
            r.Should().BeOfType<Ok>();
            dm.StartScanningCalled.Should().BeTrue();
            // Throw if we try to scan again
            _server.Awaiting(async s => await s.SendMessageAsync(new StartScanning())).Should()
                .Throw<ButtplugDeviceException>();

            var finishTask = new TaskCompletionSource<bool>();

            void ScanningFinishedHandler(object aObj, MessageReceivedEventArgs aMsg)
            {
                if (aMsg.Message is ScanningFinished)
                {
                    finishTask.TrySetResult(true);
                }
            }

            _server.MessageReceived += ScanningFinishedHandler;
            _server.SendMessageAsync(new StopScanning());
            await finishTask.Task;

            // Now we should be able to call StartScanning again.
            r = await _server.SendMessageAsync(new StartScanning());
            r.Should().BeOfType<Ok>();
        }

        [ButtplugMessageMetadata("FakeMessage", 0)]
        private class FakeMessage : ButtplugMessage
        {
            public FakeMessage(uint aId)
                : base(aId)
            {
            }
        }

        [Test]
        public void TestSendUnhandledMessage()
        {
            _server.Awaiting(async aServer => await aServer.SendMessageAsync(new FakeMessage(1))).Should().Throw<ButtplugMessageException>();
        }

        [Test]
        public async Task TestStopScanning()
        {
            var dm = new TestDeviceSubtypeManager();
            _server.AddDeviceSubtypeManager(aLogger => dm);
            var r = await _server.SendMessageAsync(new StopScanning());
            r.Should().BeOfType<Ok>();
            dm.StopScanningCalled.Should().BeTrue();
        }

        [Test]
        public async Task TestRequestServerInfo()
        {
            var s = new ButtplugServer("TestServer", 100);
            var r = await s.SendMessageAsync(new RequestServerInfo("TestClient"));

            r.Should().BeOfType<ServerInfo>();
            var info = r as ServerInfo;
            info.MajorVersion.Should().Be(Assembly.GetAssembly(typeof(ServerInfo)).GetName().Version.Major);
            info.MinorVersion.Should().Be(Assembly.GetAssembly(typeof(ServerInfo)).GetName().Version.Minor);
            info.BuildVersion.Should().Be(Assembly.GetAssembly(typeof(ServerInfo)).GetName().Version.Build);
        }

        [Test]
        public async Task TestDoNotRequestServerInfoFirst()
        {
            var s = new ButtplugServer("TestServer", 0);

            s.Awaiting(async aServer => await s.SendMessageAsync(new Core.Messages.Test("Test"))).Should().Throw<ButtplugHandshakeException>();

            var msg = await s.SendMessageAsync(new RequestServerInfo("TestClient"));
            msg.Should().BeOfType<ServerInfo>();

            msg = await s.SendMessageAsync(new Core.Messages.Test("Test"));
            msg.Should().BeOfType<Core.Messages.Test>();
        }
    }
}
