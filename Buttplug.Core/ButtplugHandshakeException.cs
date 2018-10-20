using System;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace Buttplug.Core
{
    public class ButtplugHandshakeException : ButtplugException
    {
        /// <inheritdoc />
        public ButtplugHandshakeException(string aMessage, uint aId = ButtplugConsts.SystemMsgId, Exception aInner = null) :
            base(aMessage, Error.ErrorClass.ERROR_INIT, aId, aInner)
        {
        }

        /// <inheritdoc />
        public ButtplugHandshakeException([NotNull] IButtplugLog aLogger, string aMessage, uint aId = ButtplugConsts.SystemMsgId, Exception aInner = null) :
            base(aLogger, aMessage, Error.ErrorClass.ERROR_INIT, aId, aInner)
        {
        }
    }
}