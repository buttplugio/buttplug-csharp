using System;

using Buttplug.Core.Messages;

namespace Buttplug.Core
{
    public class ButtplugMessageException : ButtplugException
    {
        /// <inheritdoc />
        public ButtplugMessageException(string aMessage, uint aId = ButtplugConsts.SystemMsgId, Exception aInner = null)
            : base(aMessage, Error.ErrorClass.ERROR_MSG, aId, aInner)
        {
        }
    }
}