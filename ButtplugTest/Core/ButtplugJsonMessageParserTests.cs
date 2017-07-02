using Buttplug.Core;
using Buttplug.Messages;
using Xunit;

namespace ButtplugTest.Core
{
    public class ButtplugJsonMessageParserTests
    {
        private readonly TestService _service = new TestService();

        [Fact]
        public void JsonConversionTest()
        {
            var m1 = new Test("ThisIsATest", ButtplugConsts.SYSTEM_MSG_ID);
            var m2 = new Test("ThisIsAnotherTest", ButtplugConsts.SYSTEM_MSG_ID);
            var msg = _service.Serialize(m1);
            Assert.True(msg.Length > 0);
            Assert.Equal("[{\"Test\":{\"TestString\":\"ThisIsATest\",\"Id\":0}}]", msg);
            ButtplugMessage[] msgs = { m1, m2 };
            msg = _service.Serialize(msgs);
            Assert.True(msg.Length > 0);
            Assert.Equal("[{\"Test\":{\"TestString\":\"ThisIsATest\",\"Id\":0}},{\"Test\":{\"TestString\":\"ThisIsAnotherTest\",\"Id\":0}}]", msg);
        }

        // Not valid JSON
        [InlineData("not a json message")]
        // Valid json object but no contents
        [InlineData("{}")]
        // Valid json but not an object
        [InlineData("[]")]
        // Not a message type
        [InlineData("[{\"NotAMessage\":{}}]")]
        // Valid json and message type but not in correct format
        [InlineData("[{\"Test\":[]}]")]
        // Valid json and message type but not in correct format
        [InlineData("[{\"Test\":{}}]")]
        // Valid json and message type but with erroneous content
        [InlineData("[{\"Test\":{\"TestString\":\"Error\",\"Id\":0}}]")]
        // Valid json and message type but with extra content
        [InlineData("[{\"Test\":{\"TestString\":\"Yup\",\"NotAField\":\"NotAValue\",\"Id\":0}}]")]
        [Theory]
        public void DeserializeIncorrectMessages(string aMsgStr)
        {
            var res = _service.Deserialize(aMsgStr);
            Assert.True(res.Length == 1);
            Assert.True(res[0] is Error);
        }

        [Fact]
        public void DeserializeCorrectMessage()
        {
            var m = _service.Deserialize("[{\"Test\":{\"TestString\":\"Test\",\"Id\":0}}]");
            Assert.True(m.Length == 1);
            switch (m[0])
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