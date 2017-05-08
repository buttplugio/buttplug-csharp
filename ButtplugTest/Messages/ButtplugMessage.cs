using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buttplug.Core;
using LanguageExt;
using Newtonsoft.Json;
using Xunit;
using Buttplug;

namespace ButtplugTest.Messages
{
    public class ButtplugMessageTests
    {
        [Fact]
        public async void RequestLogJsonTest()
        {
            var s = new ButtplugService();
            Assert.True((await s.SendMessage("{\"RequestLog\": {\"LogLevel\":\"Trace\"}}")).IsRight);
        }

        [Fact]
        public async void RequestLogWrongLevelTest()
        {
            var s = new ButtplugService();
            Assert.True((await s.SendMessage("{\"RequestLog\": {\"LogLevel\":\"NotALevel\"}}")).IsLeft);
        }
    }
}
