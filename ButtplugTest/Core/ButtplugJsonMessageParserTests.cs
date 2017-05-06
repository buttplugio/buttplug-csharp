using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buttplug.Core;
using Xunit;
using Buttplug.Messages;

namespace ButtplugTest.Core
{
    public partial class ButtplugJsonMessageParserTests
    {
        [Fact]
        public void JsonConversionTest()
        {
            var m = new TestMessage("ThisIsATest");
            var msg = ButtplugJsonMessageParser.Serialize(m);
            Assert.True(msg.IsSome);
            msg.IfSome((x) => Assert.Equal(x, "{\"TestMessage\":{\"TestString\":\"ThisIsATest\"}}"));
        }
    }
}
