using Buttplug.Core.Messages;
using FluentAssertions;
using NUnit.Framework;

namespace Buttplug.Core.Test
{
    [TestFixture]
    public class ButtplugExceptionTests
    {
        [Test]
        public void TestFromError()
        {
            ButtplugException.FromError(new Error("test", Error.ErrorClass.ERROR_DEVICE, ButtplugConsts.SystemMsgId))
                .Should().BeOfType<ButtplugDeviceException>();
            ButtplugException.FromError(new Error("test", Error.ErrorClass.ERROR_UNKNOWN, ButtplugConsts.SystemMsgId))
                .Should().BeOfType<ButtplugException>();
            ButtplugException.FromError(new Error("test", Error.ErrorClass.ERROR_INIT, ButtplugConsts.SystemMsgId))
                .Should().BeOfType<ButtplugHandshakeException>();
            ButtplugException.FromError(new Error("test", Error.ErrorClass.ERROR_MSG, ButtplugConsts.SystemMsgId))
                .Should().BeOfType<ButtplugMessageException>();
            ButtplugException.FromError(new Error("test", Error.ErrorClass.ERROR_PING, ButtplugConsts.SystemMsgId))
                .Should().BeOfType<ButtplugPingException>();
        }

        [Test]
        public void TestToErrorMessage()
        {
            var msg = new Error("test1", Error.ErrorClass.ERROR_DEVICE, 2);
            var err = ButtplugException.FromError(msg);
            err.ButtplugErrorMessage.Should().BeEquivalentTo(msg);
        }
    }
}