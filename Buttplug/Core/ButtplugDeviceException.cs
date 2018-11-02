using System;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace Buttplug.Core
{
    public class ButtplugDeviceException : ButtplugException
    {
        /// <inheritdoc />
        public ButtplugDeviceException(string aMessage, uint aId = ButtplugConsts.SystemMsgId, Exception aInner = null) :
            base(aMessage, Error.ErrorClass.ERROR_DEVICE, aId, aInner)
        {
        }

        /// <inheritdoc />
        public ButtplugDeviceException([NotNull] IButtplugLog aLogger, string aMessage, uint aId = ButtplugConsts.SystemMsgId, Exception aInner = null) :
            base(aLogger, aMessage, Error.ErrorClass.ERROR_DEVICE, aId, aInner)
        {
        }
    }
}