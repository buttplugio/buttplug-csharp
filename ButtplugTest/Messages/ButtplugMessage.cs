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
        [Fact]
        public async void RequestLogJsonTest()
        {
            var s = new TestService();
            Assert.True((await s.SendMessage("{\"RequestLog\": {\"LogLevel\":\"Trace\",\"Id\":1}}")).IsRight);
        }

        [Fact]
        public async void RequestLogWrongLevelTest()
        {
            var s = new TestService();
            Assert.True((await s.SendMessage("{\"RequestLog\": {\"LogLevel\":\"NotALevel\",\"Id\":1}}")).IsLeft);
        }

        [Fact]
        public async void CallStartScanning()
        {
            var dm = new TestDeviceManager();
            var s = new TestService(dm);
            var r = await s.SendMessage(new StartScanning());
            r.Right(x =>
                {
                    Assert.True(x is Ok);
                    Assert.True(dm.StartScanningCalled);
                })
                .Left(x =>
                {
                    Assert.True(false, $"SendMessage returned error: {x.ErrorMessage}");
                });
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
            Assert.True(r.IsLeft);
        }

        [Fact]
        public async void SerializeUnhandledMessage()
        {
            var r = ButtplugJsonMessageParser.Serialize(new FakeMessage(1));
            // Even though the message is defined outside the core library, it should at least serialize
            Assert.True(r.IsSome);
            // However it shouldn't be taken by the server.
            var s = new ButtplugService();
            Either<Error, ButtplugMessage> e = new Error("Yup", ButtplugConsts.DEFAULT_MSG_ID);
            await r.IfSomeAsync(async x => e = await s.SendMessage(x));
            Assert.True(e.IsLeft);
        }

        [Fact]
        public async void CallStopScanning()
        {
            var dm = new TestDeviceManager();
            var s = new TestService(dm);
            var r = await s.SendMessage(new StopScanning());
            r.Right(x =>
                {
                    Assert.True(x is Ok);
                    Assert.True(dm.StopScanningCalled);
                })
                .Left(x =>
                {
                    Assert.True(false, $"SendMessage returned error: {x.ErrorMessage}");
                });
        }

        [Fact]
        public async void RequestServerInfoTest()
        {
            var s = new ButtplugService();
            var results = new List<Either<Error, ButtplugMessage>>
            {
                await s.SendMessage(new RequestServerInfo()),
                await s.SendMessage("{\"RequestServerInfo\":{\"Id\":1}}")
            };
            foreach (var reply in results)
            {
                reply
                    .Right(x =>
                    {
                        switch (x)
                        {
                            case ServerInfo si:
                                Assert.True(true, "Got ServerInfo");
                                Assert.True(si.MajorVersion == Assembly.GetAssembly(typeof(ServerInfo)).GetName().Version.Major);
                                Assert.True(si.MinorVersion == Assembly.GetAssembly(typeof(ServerInfo)).GetName().Version.Minor);
                                Assert.True(si.BuildVersion == Assembly.GetAssembly(typeof(ServerInfo)).GetName().Version.Build);
                                break;

                            default:
                                Assert.True(false, $"Received message type {x.GetType()}, not ServerInfo");
                                break;
                        }
                    })
                    .Left(x =>
                    {
                        Assert.True(false, $"SendMessage returned error: {x.ErrorMessage}");
                    });
            }
        }
    }
}