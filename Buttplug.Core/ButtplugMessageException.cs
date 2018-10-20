using System;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace Buttplug.Core
{
    public class ButtplugMessageException : ButtplugException
    {
        /// <inheritdoc />
        public ButtplugMessageException(string aMessage, uint aId = ButtplugConsts.SystemMsgId, Exception aInner = null) :
            base(aMessage, Error.ErrorClass.ERROR_MSG, aId, aInner)
        {
        }

        /// <inheritdoc />
        public ButtplugMessageException([NotNull] IButtplugLog aLogger, string aMessage, uint aId = ButtplugConsts.SystemMsgId, Exception aInner = null) :
            base(aLogger, aMessage, Error.ErrorClass.ERROR_MSG, aId, aInner)
        {
        }
    }
}