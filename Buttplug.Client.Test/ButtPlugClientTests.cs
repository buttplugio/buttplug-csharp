using System;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Components.WebsocketServer;
using Buttplug.Core;
using Buttplug.Server;
using Xunit;

namespace Buttplug.Client.Test
{
    public class ButtplugClientTests : IButtplugServerFactory
    {
        public ButtplugServer GetServer()
        {
            return new ButtplugServer("Test server", 200);
        }

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

        [Fact]
        public async void TestConnection()
        {
            var server = new ButtplugWebsocketServer();
            server.StartServer(this);

            var client = new ButtplugTestClient("Test client");
            await client.Connect(new Uri("ws://localhost:12345/buttplug"));

            var msgId = client.nextMsgId;
            var res = await client.SendMsg(new Core.Messages.Test("Test string", msgId));
            Assert.True(res != null);
            Assert.True(res is Core.Messages.Test);
            Assert.True(((Core.Messages.Test)res).TestString == "Test string");
            Assert.True(((Core.Messages.Test)res).Id == msgId);

            // Check ping is working
            Thread.Sleep(400);

            msgId = client.nextMsgId;
            res = await client.SendMsg(new Core.Messages.Test("Test string", msgId));
            Assert.True(res != null);
            Assert.True(res is Core.Messages.Test);
            Assert.True(((Core.Messages.Test)res).TestString == "Test string");
            Assert.True(((Core.Messages.Test)res).Id == msgId);

            Assert.True(client.nextMsgId > 4);

            await client.RequestDeviceList();

            // Shut it down
            await client.Disconnect();
            server.StopServer();
        }
    }
}
