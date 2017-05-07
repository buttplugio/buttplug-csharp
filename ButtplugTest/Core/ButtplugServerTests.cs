using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Buttplug.Core;
using Buttplug.Messages;

namespace ButtplugTest.Core
{
    public class ButtplugServerTests
    {
        [Fact]
        public async void RejectOutgoingOnlyMessage()
        {
            Assert.True((await new ButtplugService().SendMessage(new Error("Error"))).IsLeft);
        }

        [Fact]
        public async void LoggerSettingsTest()
        {
            var gotMessage = false;
            var s = new ButtplugService();
            s.MessageReceived += (obj, msg) =>
            {
                if (msg.Message.GetType() == typeof(Log))
                {
                    gotMessage = true;
                }
            };
            // Sending error messages will always cause an error, as they are outgoing, not incoming.
            await s.SendMessage(new Error("Error"));
            Assert.False(gotMessage);
            Assert.True((await s.SendMessage(new RequestLog("Trace"))).IsRight);
            Assert.True((await s.SendMessage(new Error("Error"))).IsLeft);
            Assert.True(gotMessage);
            await s.SendMessage(new RequestLog("Off"));
            gotMessage = false;
            await s.SendMessage(new Error("Error"));
            Assert.False(gotMessage);
        }
    }
}
