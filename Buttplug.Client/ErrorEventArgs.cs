using System;
using Buttplug.Core;
using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace Buttplug.Client
{
    /// <summary>
    /// Event wrapper for a Buttplug Error message. Used when the client recieves an unhandled error,
    /// or an exception is thrown.
    /// </summary>
    public class ErrorEventArgs
    {
        /// <summary>
        /// The Buttplug Error message.
        /// </summary>
        [NotNull]
        public readonly Error Message;

        /// <summary>
        /// The exception raised.
        /// </summary>
        [NotNull]
        public readonly Exception Exception;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorEventArgs"/> class, based on an Error message.
        /// </summary>
        /// <param name="aMsg">The Buttplug Error message.</param>
        public ErrorEventArgs(Error aMsg)
        {
            Message = aMsg;
            Exception = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorEventArgs"/> class, based on the
        /// exception being raised.
        /// </summary>
        /// <param name="aException">The caught exception.</param>
        public ErrorEventArgs(Exception aException)
        {
            Exception = aException;
            Message = new Error(Exception.Message, Error.ErrorClass.ERROR_UNKNOWN, ButtplugConsts.SystemMsgId);
        }
    }
}