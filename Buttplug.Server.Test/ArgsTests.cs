// <copyright file="ArgsTests.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using NUnit.Framework;

namespace Buttplug.Server.Test
{
    [TestFixture]
    public class ArgsTests
    {
        [Test]
        public void LogExceptionEventArgsTest()
        {
            var ex = new Exception("foo");
            var arg = new LogExceptionEventArgs(ex, false, "bar");
            Assert.AreEqual(ex, arg.Ex);
            Assert.False(arg.LocalOnly);
            Assert.AreEqual("bar", arg.ErrorMessage);
        }

        [Test]
        public void MessageReceivedEventArgsTest()
        {
            var msg = new Core.Messages.Test("foo");
            var arg = new MessageReceivedEventArgs(msg);
            Assert.AreEqual(msg, arg.Message);
        }

        [Test]
        public void ButtplugLogMessageEventArgsTest()
        {
            var arg = new ButtplugLogMessageEventArgs(ButtplugLogLevel.Info, "foo");
            Assert.AreEqual("foo", arg.LogMessage.LogMessage);
            Assert.AreEqual(ButtplugLogLevel.Info, arg.LogMessage.LogLevel);
        }
    }
}
