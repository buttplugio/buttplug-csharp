﻿using System;
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

        /// <summary>
        /// Creates a ButtplugException.
        /// </summary>
        /// <param name="aMessage">Exception message.</param>
        /// <param name="aClass">Exception class, based on Buttplug Error Message Classes. (https://buttplug-spec.docs.buttplug.io/status.html#error)</param>
        /// <param name="aId">Message ID for the resulting Buttplug Error Message.</param>
        public ButtplugException(string aMessage, Error.ErrorClass aClass, uint aId) :
            base(aMessage)
        {
            ButtplugErrorMessage = new Error(aMessage, aClass, aId);
        }

        /// <summary>
        /// Creates a ButtplugException.
        /// </summary>
        /// <param name="aLogger">Logger to log exception error message through (gives type context for the message).</param>
        /// <param name="aMessage">Exception message.</param>
        /// <param name="aClass">Exception class, based on Buttplug Error Message Classes. (https://buttplug-spec.docs.buttplug.io/status.html#error)</param>
        /// <param name="aId">Message ID for the resulting Buttplug Error Message.</param>
        public ButtplugException([NotNull] IButtplugLog aLogger, string aMessage, Error.ErrorClass aClass, uint aId) :
            this(aMessage, aClass, aId)
        {
            if (aLogger == null)
            {
                throw new ArgumentNullException(nameof(aLogger));
            }

            aLogger.Error(aMessage);
        }
    }
}
