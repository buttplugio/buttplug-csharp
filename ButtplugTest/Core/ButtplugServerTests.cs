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
            Assert.False(await new ButtplugService().SendMessage(new Error("Error")));
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
            await s.SendMessage(new Error("Error"));
            Assert.False(gotMessage);
            await s.SendMessage(new RequestLog("Trace"));
            await s.SendMessage(new Error("Error"));
            Assert.True(gotMessage);
            await s.SendMessage(new RequestLog("Off"));
            gotMessage = false;
            await s.SendMessage(new Error("Error"));
            Assert.False(gotMessage);
        }
    }
}
