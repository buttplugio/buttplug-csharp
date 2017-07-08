using System;
using System.Threading;
using Buttplug.Core;
using Buttplug.Messages;
using ButtplugClient.Core;
using ButtplugWebsockets;
using Xunit;
using System.Threading.Tasks;

namespace ButtplugClientTest
{
    public class ButtplugClientTests : IButtplugServiceFactory
    {
        public ButtplugService GetService()
        {
            return new ButtplugService("Test service", 100);
        }

        class ButtplugTestClient : ButtplugWSClient
        {
            public ButtplugTestClient(string aClientName) : base(aClientName)
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

            Console.WriteLine("test msg 1");
            var msgId = client.nextMsgId;
            var res = await client.SendMsg(new Test("Test string", msgId));
            Assert.True(res != null);
            Assert.True(res is Test);
            Assert.True(((Test)res).TestString == "Test string");
            Assert.True(((Test)res).Id == msgId);

            // Check ping is working
            Thread.Sleep(400);

            Console.WriteLine("test msg 2");
            msgId = client.nextMsgId;
            res = await client.SendMsg(new Test("Test string", msgId));
            Assert.True(res != null);
            Assert.True(res is Test);
            Assert.True(((Test)res).TestString == "Test string");
            Assert.True(((Test)res).Id == msgId);

            Assert.True(client.nextMsgId > 4);

            Console.WriteLine("FINISHED CLIENT DISCONNECT");

            // Shut it down
            await client.Disconnect();
            server.StopServer();
        }
    }
}
