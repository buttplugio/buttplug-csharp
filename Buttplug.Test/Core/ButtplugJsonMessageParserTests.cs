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
using Buttplug.Core.Messages;
using FluentAssertions;
using NUnit.Framework;

namespace Buttplug.Core.Test
{
    [TestFixture]
    public class ButtplugJsonMessageParserTests
    {
        private ButtplugJsonMessageParser _parser;

        [OneTimeSetUp]
        public void SetUp()
        {
            _parser = new ButtplugJsonMessageParser();
        }

        // ReSharper disable once UnusedMember.Global
        [Datapoints]
        public string[] TestData =
        {
            // Not valid JSON
            "not a json message",

            // Valid json object but no contents
            "{}",

            // Not a message type
            "[{\"NotAMessage\":{}}]",

            // Valid json and message type but not in correct format
            "[{\"Ok\":[]}]",

            // Valid json and message type but not in correct format
            "[{\"Ok\":{}}]",

            // Valid json and message type but with erroneous content
            "[{\"Ok\":{\"Id\":\"Test\"}}]",

            // Valid json and message type but with extra content
            "[{\"Ok\":{\"NotAField\":\"NotAValue\",\"Id\":0}}]",
        };

        [Theory]
        public void DeserializeIncorrectMessages(string msgStr)
        {
            _parser.Invoking(parser => parser.Deserialize(msgStr)).Should().Throw<ButtplugMessageException>();
        }

        [Test]
        public void DeserializePartiallyInvalidMessageArray()
        {
            _parser
                .Invoking(parser =>
                    parser.Deserialize(
                        "[{\"Test\":{\"TestString\":\"Test\",\"Id\":0}},{\"Test\":{\"TestString\":\"Error\",\"Id\":1}},{\"Test\":{\"TestString\":\"Test\",\"Id\":1}}]"))
                .Should()
                .Throw<ButtplugMessageException>();
        }

        [Test]
        public void DeserializeConcatenatedMessages()
        {
            var msgs = _parser.Deserialize("[{\"Ok\":{\"Id\":1}}][{\"Ok\":{\"Id\":2}}]").ToArray();
            msgs.Length.Should().Be(2);
            foreach (var msg in msgs)
            {
                msg.Should().BeOfType<Ok>();
            }
        }

        [Test]
        public void DeserializeCorrectMessage()
        {
            var msgs = _parser.Deserialize("[{\"Ok\":{\"Id\":1}}]").ToArray();
            msgs.Length.Should().Be(1);
            msgs[0].Should().BeOfType<Ok>();
        }

        [ButtplugMessageMetadata("FakeMessage")]
        private class FakeMessage : ButtplugMessage
        {
            public FakeMessage(uint id)
                : base(id)
            {
            }
        }

        private class FakeMessageNoMetadata : ButtplugMessage
        {
            public FakeMessageNoMetadata(uint id)
                : base(id)
            {
            }
        }

        [Test]
        public void TestParseUnhandledMessage()
        {
            Action a = () => _parser.Serialize(new FakeMessage(0));
            a.Should().Throw<ButtplugMessageException>();
        }

        [Test]
        public void TestParseUnhandledMessageWithNoMetadata()
        {
            Action a = () => _parser.Serialize(new FakeMessageNoMetadata(0));
            a.Should().Throw<ButtplugMessageException>();
        }
    }
}
