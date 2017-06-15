using Buttplug.Core;
using Buttplug.Messages;
using ButtplugClient.Core;
using ButtplugWebsockets;
using System;
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
            client.Connect(new Uri("ws://localhost:12345/buttplug")).GetAwaiter().GetResult();
            var res = client.SendMessage(new Test("Test string", 6)).GetAwaiter().GetResult();
            Assert.True(res.Length == 1);
            Assert.True(res[0] is Test);
            Assert.True(((Test)res[0]).TestString == "Test string");
            Assert.True(((Test)res[0]).Id == 6);
        }

    }
}
