// <copyright file="ButtplugClientException.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Buttplug.Core;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace Buttplug.Client
{
    public class ButtplugClientException : ButtplugException
    {
        /// <inheritdoc cref="ButtplugException"/>
        /// <summary>
        /// Initializes a new instance of the <see cref="ButtplugClientException"/> class.
        /// </summary>
        /// <param name="aMessage">Exception message.</param>
        /// <param name="aClass">Exception class, based on Buttplug Error Message Classes. See https://buttplug-spec.docs.buttplug.io/status.html#error for more info.</param>
        /// <param name="aId">Message ID for the resulting Buttplug Error Message.</param>
        /// <param name="aInner">Optional inner exception.</param>
        public ButtplugClientException(string aMessage, Error.ErrorClass aClass, uint aId, Exception aInner = null)
            : base(aMessage, aClass, aId, aInner)
        {
        }

        /// <inheritdoc cref="ButtplugException"/>
        /// <summary>
        /// Initializes a new instance of the <see cref="ButtplugClientException"/> class.
        /// </summary>
        /// <param name="aLogger">Logger to log exception error message through (gives type context for the message).</param>
        /// <param name="aMessage">Exception message.</param>
        /// <param name="aClass">Exception class, based on Buttplug Error Message Classes. See https://buttplug-spec.docs.buttplug.io/status.html#error for more info.</param>
        /// <param name="aId">Message ID for the resulting Buttplug Error Message.</param>
        /// <param name="aInner">Optional inner exception.</param>
        public ButtplugClientException(IButtplugLog aLogger, string aMessage, Error.ErrorClass aClass, uint aId, Exception aInner = null)
            : this(aMessage, aClass, aId, aInner)
        {
            aLogger?.Error(aMessage);
        }

        public ButtplugClientException(IButtplugLog aLogger, Error aMsg)
            : this(aLogger, aMsg.ErrorMessage, aMsg.ErrorCode, aMsg.Id)
        {
        }
    }
}