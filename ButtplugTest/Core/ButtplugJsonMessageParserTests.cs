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

        [Fact]
        public void JsonConversionFailureTest()
        {
            var m = new TestMessage("Error");
            var msg = ButtplugJsonMessageParser.Serialize(m);
            Assert.True(msg.IsNone);
        }

        // Not valid JSON
        [InlineData("not a json message")]
        // Valid json object but no contents
        [InlineData("{}")]
        // Valid json but not an object
        [InlineData("[]")]
        // Not a message type
        [InlineData("{ \"NotAMessage\": {}}")]
        // Valid json and message type but not in correct format
        [InlineData("{ \"TestMessage\" : []}")]
        // Valid json and message type but not in correct format
        [InlineData("{ \"TestMessage\" : {}}")]
        // Valid json and message type but with erroneous content
        [InlineData("{ \"TestMessage\" : {\"TestString\":\"Error\"}}")]
        [Theory]
        public void DeserializeIncorrectMessages(string x)
        {
            var p = new ButtplugJsonMessageParser();
            Assert.True(p.Deserialize(x).IsLeft);
        }
    }
}
