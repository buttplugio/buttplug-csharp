using Buttplug.Core;
using Buttplug.Messages;
using Xunit;

namespace ButtplugTest.Core
{
    public class ButtplugJsonMessageParserTests
    {
        private ButtplugService service = new ButtplugService();

        [Fact]
        public void JsonConversionTest()
        {
            var m = new Buttplug.Messages.Test("ThisIsATest", ButtplugConsts.SYSTEM_MSG_ID);
            var msg = service.Serialize(m);
            
            Assert.True(msg.Length > 0);
            Assert.Equal("[{\"Test\":{\"TestString\":\"ThisIsATest\",\"Id\":0}}]", msg);
        }

        // Not valid JSON
        [InlineData("not a json message")]
        // Valid json object but no contents
        [InlineData("{}")]
        // Valid json but not an object
        [InlineData("[]")]
        // Not a message type
        [InlineData("{\"NotAMessage\":{}}")]
        // Valid json and message type but not in correct format
        [InlineData("{\"Test\":[]}")]
        // Valid json and message type but not in correct format
        [InlineData("{\"Test\":{}}")]
        // Valid json and message type but with erroneous content
        [InlineData("{\"Test\":{\"TestString\":\"Error\",\"Id\":0}}")]
        // Valid json and message type but with extra content
        [InlineData("{\"Test\":{\"TestString\":\"Yup\",\"NotAField\":\"NotAValue\",\"Id\":0}}")]
        [Theory]
        public void DeserializeIncorrectMessages(string x)
        {
            Assert.True(service.Deserialize(x) is Error);
        }

        [Fact]
        public void DeserializeCorrectMessage()
        {
            var m = service.Deserialize("[{\"Test\":{\"TestString\":\"Test\",\"Id\":0}}]");
            switch (m)
            {
                case Error e:
                    Assert.True(false, $"Got Error: {e.ErrorMessage}");
                    break;
                case Test tm:
                    Assert.True(tm.TestString == "Test");
                    break;
                default:
                    Assert.True(false, $"Got wrong message type {m.GetType().Name}");
                    break;
            }
        }
    }
}