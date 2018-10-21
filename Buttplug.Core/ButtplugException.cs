using System;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace Buttplug.Core
{
    public class ButtplugException : Exception
    {
        /// <summary>
        /// Buttplug Error Message representing the exception. Can be used for relaying remote exceptions.
        /// </summary>
        public Error ButtplugErrorMessage { get; }

        public static ButtplugException FromError(IButtplugLog aLogger, Error aMsg)
        {
            ButtplugUtils.ArgumentNotNull(aLogger, nameof(aLogger));
            aLogger.Error(aMsg.ErrorMessage);
            return FromError(aMsg);
        }

        public static ButtplugException FromError(Error aMsg)
        {
            switch (aMsg.ErrorCode)
            {
                case Error.ErrorClass.ERROR_DEVICE:
                    return new ButtplugDeviceException(aMsg.ErrorMessage, aMsg.Id);
                case Error.ErrorClass.ERROR_INIT:
                    return new ButtplugHandshakeException(aMsg.ErrorMessage, aMsg.Id);
                case Error.ErrorClass.ERROR_MSG:
                    return new ButtplugMessageException(aMsg.ErrorMessage, aMsg.Id);
                case Error.ErrorClass.ERROR_PING:
                    return new ButtplugPingException(aMsg.ErrorMessage, aMsg.Id);
                case Error.ErrorClass.ERROR_UNKNOWN:
                    return new ButtplugException(aMsg.ErrorMessage, aMsg.Id);
                default:
                    return new ButtplugException(aMsg.ErrorMessage, aMsg.Id);
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Creates a ButtplugException.
        /// </summary>
        /// <param name="aMessage">Exception message.</param>
        /// <param name="aId">Message ID for the resulting Buttplug Error Message.</param>
        /// <param name="aInner">Optional inner exception.</param>
        public ButtplugException(string aMessage, uint aId = ButtplugConsts.SystemMsgId, Exception aInner = null)
            : this(aMessage, Error.ErrorClass.ERROR_UNKNOWN, aId, aInner)
        {
        }

        /// <inheritdoc />
        /// <summary>
        /// Creates a ButtplugException.
        /// </summary>
        /// <param name="aMessage">Exception message.</param>
        /// <param name="aClass">Exception class, based on Buttplug Error Message Classes. (https://buttplug-spec.docs.buttplug.io/status.html#error)</param>
        /// <param name="aId">Message ID for the resulting Buttplug Error Message.</param>
        /// <param name="aInner">Optional inner exception.</param>
        public ButtplugException(string aMessage, Error.ErrorClass aClass = Error.ErrorClass.ERROR_UNKNOWN, uint aId = ButtplugConsts.SystemMsgId, Exception aInner = null) 
            : base(aMessage, aInner)
        {
            ButtplugErrorMessage = new Error(aMessage, aClass, aId);
        }

        /// <inheritdoc />
        /// <summary>
        /// Creates a ButtplugException.
        /// </summary>
        /// <param name="aLogger">Logger to log exception error message through (gives type context for the message).</param>
        /// <param name="aMessage">Exception message.</param>
        /// <param name="aClass">Exception class, based on Buttplug Error Message Classes. (https://buttplug-spec.docs.buttplug.io/status.html#error)</param>
        /// <param name="aId">Message ID for the resulting Buttplug Error Message.</param>
        /// <param name="aInner">Optional inner exception.</param>
        public ButtplugException([NotNull] IButtplugLog aLogger, string aMessage, Error.ErrorClass aClass = Error.ErrorClass.ERROR_UNKNOWN, uint aId = ButtplugConsts.SystemMsgId, Exception aInner = null) 
            : this(aMessage, aClass, aId, aInner)
        {
            ButtplugUtils.ArgumentNotNull(aLogger, nameof(aLogger));
            aLogger.Error(aMessage);
        }
    }
}
