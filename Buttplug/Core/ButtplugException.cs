// <copyright file="ButtplugException.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;

using Buttplug.Core.Messages;

namespace Buttplug.Core
{
    public class ButtplugException : Exception
    {
        /// <summary>
        /// Buttplug Error Message representing the exception. Can be used for relaying remote exceptions.
        /// </summary>
        public Error ButtplugErrorMessage { get; }

        public static ButtplugException FromError(Error msg)
        {
            switch (msg.ErrorCode)
            {
                case Error.ErrorClass.ERROR_DEVICE:
                    return new ButtplugDeviceException(msg.ErrorMessage, msg.Id);
                case Error.ErrorClass.ERROR_INIT:
                    return new ButtplugHandshakeException(msg.ErrorMessage, msg.Id);
                case Error.ErrorClass.ERROR_MSG:
                    return new ButtplugMessageException(msg.ErrorMessage, msg.Id);
                case Error.ErrorClass.ERROR_PING:
                    return new ButtplugPingException(msg.ErrorMessage, msg.Id);
                case Error.ErrorClass.ERROR_UNKNOWN:
                    return new ButtplugException(msg.ErrorMessage, msg.Id);
                default:
                    return new ButtplugException(msg.ErrorMessage, msg.Id);
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Creates a ButtplugException.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="id">Message ID for the resulting Buttplug Error Message.</param>
        /// <param name="inner">Optional inner exception.</param>
        public ButtplugException(string message, uint id = ButtplugConsts.SystemMsgId, Exception inner = null)
            : this(message, Error.ErrorClass.ERROR_UNKNOWN, id, inner)
        {
        }

        /// <inheritdoc />
        /// <summary>
        /// Creates a ButtplugException.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="class">Exception class, based on Buttplug Error Message Classes. (https://buttplug-spec.docs.buttplug.io/status.html#error).</param>
        /// <param name="id">Message ID for the resulting Buttplug Error Message.</param>
        /// <param name="inner">Optional inner exception.</param>
        public ButtplugException(string message, Error.ErrorClass err = Error.ErrorClass.ERROR_UNKNOWN, uint id = ButtplugConsts.SystemMsgId, Exception inner = null)
            : base(message, inner)
        {
            ButtplugErrorMessage = new Error(message, err, id);
        }
    }
}
