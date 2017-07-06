using System;
using System.Threading;
using Buttplug.Core;
using Buttplug.Messages;
using ButtplugClient.Core;
using ButtplugWebsockets;
using Xunit;

namespace ButtplugClientTest
{
    public class ButtplugClientTests : IButtplugServiceFactory
    {
        public ButtplugService GetService()
        {
            return new ButtplugService("Test service", 100);
        }

        [Fact]
        public void TestConnection()
        {
            var server = new ButtplugWebsocketServer();
            server.StartServer(this);

            var client = new ButtplugWSClient("Test client");
            client.Connect(new Uri("ws://localhost:12345/buttplug")).Wait();

            var msgId = client.nextMsgId;
            var res = client.SendMessage(new Test("Test string", msgId)).GetAwaiter().GetResult();
            Assert.True(res != null);
            Assert.True(res is Test);
            Assert.True(((Test)res).TestString == "Test string");
            Assert.True(((Test)res).Id == msgId);

            // Check ping is working
            Thread.Sleep(200);

            msgId = client.nextMsgId;
            res = client.SendMessage(new Test("Test string", msgId)).GetAwaiter().GetResult();
            Assert.True(res != null);
            Assert.True(res is Test);
            Assert.True(((Test)res).TestString == "Test string");
            Assert.True(((Test)res).Id == msgId);

            server.StopServer();
        }
    }
}
