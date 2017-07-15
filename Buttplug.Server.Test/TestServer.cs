using System.Collections.Generic;
using Buttplug.Core;
using Buttplug.Core.Messages;
using Buttplug.Server;
using NLog;
using NLog.Config;
using NLog.Targets;
using Xunit;

namespace Buttplug.Server.Test
{
    internal class TestServer : ButtplugServer
    {
        public readonly List<string> OutgoingAsync = new List<string>();

        // Set MaxPingTime to zero (infinite ping/ping checks off) by default for tests
        public TestServer(uint aMaxPingTime = 0)
            : base("Test Server", aMaxPingTime)
        {
            // Build ourselves an NLog manager just so we can see what's going on.
            var dt = new DebuggerTarget();
            LogManager.Configuration = new LoggingConfiguration();
            LogManager.Configuration.AddTarget("debugger", dt);
            LogManager.Configuration.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, dt));
            LogManager.Configuration = LogManager.Configuration;

            // Send RequestServerInfo message now, to save us having to do it in every test.
            var t = SendMessage(new RequestServerInfo("TestClient"));
            t.Wait();
            Assert.True(t.Result is ServerInfo);
        }

        public void OnMessageReceived(object aObj, MessageReceivedEventArgs aEvent)
        {
            OutgoingAsync.Add(Serialize(aEvent.Message));
        }
    }
}
