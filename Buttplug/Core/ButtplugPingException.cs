using System;

using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace Buttplug.Core
{
    public class ButtplugPingException : ButtplugException
    {
        /// <inheritdoc />
        public ButtplugPingException(string aMessage, uint aId = ButtplugConsts.SystemMsgId, Exception aInner = null)
            : base(aMessage, Error.ErrorClass.ERROR_PING, aId, aInner)
        {
        }
    }
}