using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Buttplug.Core;
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
