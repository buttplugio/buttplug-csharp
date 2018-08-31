// <copyright file="ButtplugJsonMessageParserTests.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using NUnit.Framework;

namespace Buttplug.Core.Test
{
    [TestFixture]
    public class ButtplugJsonMessageParserTests
    {
        private ButtplugLogManager _logManager;
        private ButtplugJsonMessageParser _parser;

        [OneTimeSetUp]
        public void SetUp()
        {
            _logManager = new ButtplugLogManager();
            _parser = new ButtplugJsonMessageParser(_logManager);
        }

        [Test]
        public void JsonConversionTest()
        {
            var m1 = new Messages.Test("ThisIsATest", ButtplugConsts.SystemMsgId);
            var m2 = new Messages.Test("ThisIsAnotherTest", ButtplugConsts.SystemMsgId);
            var msg = _parser.Serialize(m1, 0);
            Assert.True(msg.Length > 0);
            Assert.AreEqual("[{\"Test\":{\"TestString\":\"ThisIsATest\",\"Id\":0}}]", msg);
            ButtplugMessage[] msgs = { m1, m2 };
            msg = _parser.Serialize(msgs, 0);
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
            var res = _parser.Deserialize(aMsgStr);
            Assert.True(res.Length == 1);
            Assert.True(res[0] is Error);
        }

        [Test]
        public void DeserializeConcatenatedMessages()
        {
            var m = _parser.Deserialize("[{\"Test\":{\"TestString\":\"Test\",\"Id\":0}}][{\"Test\":{\"TestString\":\"Test\",\"Id\":1}}]");
            Assert.True(m.Length == 2);
            foreach (var msg in m)
            {
                switch (msg)
                {
                    case Error e:
                        Assert.True(false, $"Got Error: {e.ErrorMessage}");
                        break;
                    case Messages.Test tm:
                        Assert.True(tm.TestString == "Test");
                        break;
                    default:
                        Assert.True(false, $"Got wrong message type {msg.GetType().Name}");
                        break;
                }
            }
        }

        [Test]
        public void DeserializeCorrectMessage()
        {
            var m = _parser.Deserialize("[{\"Test\":{\"TestString\":\"Test\",\"Id\":0}}]");
            Assert.True(m.Length == 1);
            switch (m[0])
            {
                case Error e:
                    Assert.True(false, $"Got Error: {e.ErrorMessage}");
                    break;
                case Messages.Test tm:
                    Assert.True(tm.TestString == "Test");
                    break;
                default:
                    Assert.True(false, $"Got wrong message type {m.GetType().Name}");
                    break;
            }
        }
    }
}
