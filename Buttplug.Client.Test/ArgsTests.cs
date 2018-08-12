// <copyright file="ArgsTests.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using NUnit.Framework;

namespace Buttplug.Client.Test
{
    [TestFixture]
    public class ArgsTests
    {
        [Test]
        public void ErrorEventArgsTest1()
        {
            var arg = new ErrorEventArgs(
                new Core.Messages.Error("foo", Core.Messages.Error.ErrorClass.ERROR_UNKNOWN, 0));
            Assert.AreEqual("foo", arg.Message.ErrorMessage);
            Assert.Null(arg.Exception);
        }

        [Test]
        public void ErrorEventArgsTest2()
        {
            var arg = new ErrorEventArgs(new Exception("bar"));
            Assert.AreEqual("bar", arg.Message.ErrorMessage);
            Assert.AreEqual("bar", arg.Exception.Message);
        }

        [Test]
        public void LogEventArgsTest()
        {
            var arg = new LogEventArgs(new Core.Messages.Log(Core.ButtplugLogLevel.Debug, "test"));
            Assert.AreEqual("test", arg.Message.LogMessage);
            Assert.AreEqual(Core.ButtplugLogLevel.Debug, arg.Message.LogLevel);
        }
    }
}
