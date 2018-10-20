using System;
using Buttplug.Core;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace Buttplug.Server
{
    public class ButtplugServerException : ButtplugException
    {
        /// <inheritdoc />
        public ButtplugServerException(string aMessage, Error aErrorMsg)
            : this(aMessage, aErrorMsg.ErrorCode, aErrorMsg.Id)
        {
        }

        /// <inheritdoc />
        public ButtplugServerException(string aMessage, Error.ErrorClass aClass, uint aId, Exception aInner = null)
            : base(aMessage, aClass, aId, aInner)
        {
        }

        /// <inheritdoc />
        public ButtplugServerException([NotNull] IButtplugLog aLogger, string aMessage, Error.ErrorClass aClass, uint aId, Exception aInner = null)
            : base(aLogger, aMessage, aClass, aId, aInner)
        {
        }
    }
}