// <copyright file="ArgsTests.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

// Test file, disable ConfigureAwait checking.
// ReSharper disable ConsiderUsingConfigureAwait

using System.Diagnostics.CodeAnalysis;
using Buttplug.Core.Logging;
using FluentAssertions;
using NUnit.Framework;

namespace Buttplug.Client.Test
{
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Test classes can skip documentation requirements")]
    [TestFixture]
    public class ArgsTests
    {
        [Test]
        public void LogEventArgsTest()
        {
            var arg = new LogEventArgs(new Core.Messages.Log(ButtplugLogLevel.Debug, "test"));
            arg.Message.LogMessage.Should().Be("test");
            arg.Message.LogLevel.Should().Be(ButtplugLogLevel.Debug);
        }
    }
}
