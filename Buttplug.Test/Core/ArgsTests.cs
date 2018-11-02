// <copyright file="ArgsTests.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Diagnostics.CodeAnalysis;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using FluentAssertions;
using NUnit.Framework;

namespace Buttplug.Server.Test
{
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Test classes can skip documentation requirements")]
    [TestFixture]
    public class ArgsTests
    {
        [Test]
        public void MessageReceivedEventArgsTest()
        {
            var msg = new Core.Messages.Test("foo");
            var arg = new MessageReceivedEventArgs(msg);
            msg.Should().Be(arg.Message);
        }

        [Test]
        public void ButtplugLogMessageEventArgsTest()
        {
            var arg = new ButtplugLogMessageEventArgs(ButtplugLogLevel.Info, "foo");
            arg.LogMessage.LogMessage.Should().Be("foo");
            arg.LogMessage.LogLevel.Should().Be(ButtplugLogLevel.Info);
        }
    }
}
