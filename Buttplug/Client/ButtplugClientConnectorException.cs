// <copyright file="ButtplugClientConnectorException.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Buttplug.Core;

using Buttplug.Core.Messages;

namespace Buttplug.Client
{
    public class ButtplugClientConnectorException : ButtplugException
    {
        /// <inheritdoc cref="ButtplugException" />
        /// <summary>
        /// Initializes a new instance of the <see cref="ButtplugClientConnectorException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="class">Exception class, based on Buttplug Error Message Classes. See https://buttplug-spec.docs.buttplug.io/status.html#error for more info.</param>
        /// <param name="id">Message ID for the resulting Buttplug Error Message.</param>
        /// <param name="inner">Optional inner exception.</param>
        public ButtplugClientConnectorException(string message, Exception inner = null)
            : base(message, Error.ErrorClass.ERROR_UNKNOWN, ButtplugConsts.SystemMsgId, inner)
        {
        }
    }
}