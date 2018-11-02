// <copyright file="ButtplugClientConnectorException.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Buttplug.Core;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;

namespace Buttplug.Client
{
    public class ButtplugClientConnectorException : ButtplugException
    {
        /// <inheritdoc cref="ButtplugException" />
        /// <summary>
        /// Initializes a new instance of the <see cref="ButtplugClientConnectorException"/> class.
        /// </summary>
        /// <param name="aMessage">Exception message.</param>
        /// <param name="aClass">Exception class, based on Buttplug Error Message Classes. See https://buttplug-spec.docs.buttplug.io/status.html#error for more info.</param>
        /// <param name="aId">Message ID for the resulting Buttplug Error Message.</param>
        /// <param name="aInner">Optional inner exception.</param>
        public ButtplugClientConnectorException(string aMessage, Exception aInner = null)
            : base(aMessage, Error.ErrorClass.ERROR_UNKNOWN, ButtplugConsts.SystemMsgId, aInner)
        {
        }

        /// <inheritdoc cref="ButtplugException" />
        /// <summary>
        /// Initializes a new instance of the <see cref="ButtplugClientConnectorException"/> class.
        /// </summary>
        /// <param name="aLogger">Logger to log exception error message through (gives type context for the message).</param>
        /// <param name="aMessage">Exception message.</param>
        /// <param name="aClass">Exception class, based on Buttplug Error Message Classes. See https://buttplug-spec.docs.buttplug.io/status.html#error for more info.</param>
        /// <param name="aId">Message ID for the resulting Buttplug Error Message.</param>
        /// <param name="aInner">Optional inner exception.</param>
        public ButtplugClientConnectorException(IButtplugLog aLogger, string aMessage, Exception aInner = null)
            : base(aLogger, aMessage, Error.ErrorClass.ERROR_UNKNOWN, ButtplugConsts.SystemMsgId, aInner)
        {
        }
    }
}