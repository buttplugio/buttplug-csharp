using Buttplug.Core;
using Buttplug.Core.Messages;
using NUnit.Framework;

namespace Buttplug.Server.Test
{
    [TestFixture]
    public class ButtplugJsonMessageParserTests
    {
        private readonly TestServer _server = new TestServer();

        [Test]
        public void JsonConversionTest()
        {
            var m1 = new Core.Messages.Test("ThisIsATest", ButtplugConsts.SystemMsgId);
            var m2 = new Core.Messages.Test("ThisIsAnotherTest", ButtplugConsts.SystemMsgId);
            var msg = _server.Serialize(m1);
            Assert.True(msg.Length > 0);
            Assert.AreEqual("[{\"Test\":{\"TestString\":\"ThisIsATest\",\"Id\":0}}]", msg);
            ButtplugMessage[] msgs = { m1, m2 };
            msg = _server.Serialize(msgs);
            Assert.True(msg.Length > 0);
            Assert.AreEqual("[{\"Test\":{\"TestString\":\"ThisIsATest\",\"Id\":0}},{\"Test\":{\"TestString\":\"ThisIsAnotherTest\",\"Id\":0}}]", msg);
        }

        [Datapoints]
        public string[] TestData =
        {
            // Not valid JSON
            "not a json message",

            // Valid json object but no contents
            "{}",

            // Valid json but not an object
            "[]",

            // Not a message type
            "[{\"NotAMessage\":{}}]",

            // Valid json and message type but not in correct format
            "[{\"Test\":[]}]",

            // Valid json and message type but not in correct format
            "[{\"Test\":{}}]",

            // Valid json and message type but with erroneous content
            "[{\"Test\":{\"TestString\":\"Error\",\"Id\":0}}]",

            // Valid json and message type but with extra content
            "[{\"Test\":{\"TestString\":\"Yup\",\"NotAField\":\"NotAValue\",\"Id\":0}}]",
        };

        [Theory]
        public void DeserializeIncorrectMessages(string aMsgStr)
        {
            var res = _server.Deserialize(aMsgStr);
            Assert.True(res.Length == 1);
            Assert.True(res[0] is Error);
        }

        [Test]
        public void DeserializeCorrectMessage()
        {
            var m = _server.Deserialize("[{\"Test\":{\"TestString\":\"Test\",\"Id\":0}}]");
            Assert.True(m.Length == 1);
            switch (m[0])
            {
                case Error e:
                    Assert.True(false, $"Got Error: {e.ErrorMessage}");
                    break;
                case Core.Messages.Test tm:
                    Assert.True(tm.TestString == "Test");
                    break;
                default:
                    Assert.True(false, $"Got wrong message type {m.GetType().Name}");
                    break;
            }
        }
    }
}
