// <copyright file="ButtplugJsonMessageParserTests.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

// Test file, disable ConfigureAwait checking.
// ReSharper disable ConsiderUsingConfigureAwait

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using FluentAssertions;
using JetBrains.Annotations;
using NUnit.Framework;

namespace Buttplug.Core.Test
{
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Test classes can skip documentation requirements")]
    [TestFixture]
    public class ButtplugJsonMessageParserTests
    {
        [NotNull]
        private ButtplugLogManager _logManager;
        [NotNull]
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
            msg.Length.Should().BeGreaterThan(0);
            msg.Should().Be("[{\"Test\":{\"TestString\":\"ThisIsATest\",\"Id\":0}}]");
            ButtplugMessage[] msgs = { m1, m2 };
            msg = _parser.Serialize(msgs, 0);
            msg.Length.Should().BeGreaterThan(0);
            msg.Should().Be("[{\"Test\":{\"TestString\":\"ThisIsATest\",\"Id\":0}},{\"Test\":{\"TestString\":\"ThisIsAnotherTest\",\"Id\":0}}]");
        }

        // ReSharper disable once UnusedMember.Global
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
            _parser.Invoking(aParser => aParser.Deserialize(aMsgStr)).Should().Throw<ButtplugMessageException>();
        }

        private void CheckValidTestMessage([NotNull] ButtplugMessage aMsg)
        {
            aMsg.Should().BeOfType<Messages.Test>();
            (aMsg as Messages.Test).TestString.Should().Be("Test");
        }

        [Test]
        public void DeserializePartiallyInvalidMessageArray()
        {
            _parser
                .Invoking(aParser =>
                    aParser.Deserialize(
                        "[{\"Test\":{\"TestString\":\"Test\",\"Id\":0}},{\"Test\":{\"TestString\":\"Error\",\"Id\":1}},{\"Test\":{\"TestString\":\"Test\",\"Id\":1}}]"))
                .Should()
                .Throw<ButtplugMessageException>();
        }

        [Test]
        public void DeserializeConcatenatedMessages()
        {
            var msgs = _parser.Deserialize("[{\"Test\":{\"TestString\":\"Test\",\"Id\":0}}][{\"Test\":{\"TestString\":\"Test\",\"Id\":1}}]").ToArray();
            msgs.Length.Should().Be(2);
            foreach (var msg in msgs)
            {
                CheckValidTestMessage(msg);
            }
        }

        [Test]
        public void DeserializeCorrectMessage()
        {
            var msgs = _parser.Deserialize("[{\"Test\":{\"TestString\":\"Test\",\"Id\":0}}]").ToArray();
            msgs.Length.Should().Be(1);
            CheckValidTestMessage(msgs[0]);
        }

        [ButtplugMessageMetadata("FakeMessage", 0)]
        private class FakeMessage : ButtplugMessage
        {
            public FakeMessage(uint aId)
                : base(aId)
            {
            }
        }

        private class FakeMessageNoMetadata : ButtplugMessage
        {
            public FakeMessageNoMetadata(uint aId)
                : base(aId)
            {
            }
        }

        [Test]
        public void TestParseUnhandledMessage()
        {
            Action a = () => _parser.Serialize(new FakeMessage(0), ButtplugConsts.CurrentSpecVersion);
            a.Should().Throw<ButtplugMessageException>();
        }

        [Test]
        public void TestParseUnhandledMessageWithNoMetadata()
        {
            Action a = () => _parser.Serialize(new FakeMessageNoMetadata(0), ButtplugConsts.CurrentSpecVersion);
            a.Should().Throw<ArgumentException>();
        }
    }
}
