using System;
using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace Buttplug.Core.Logging
{
    /// <summary>
    /// Extension methods for <see cref="IButtplugLog"/>.
    /// </summary>
    public static class ButtplugLogExtensions
    {
        /// <summary>
        /// Logs an <see cref="Error"/> Buttplug message as an error level log message.
        /// </summary>
        /// <param name="aLogger">Logger to use for message</param>
        /// <param name="aErrorMsg">Error Buttplug message</param>
        public static void LogErrorMsg([NotNull] this IButtplugLog aLogger, [NotNull] Error aErrorMsg)
        {
            if (aLogger == null) throw new ArgumentNullException(nameof(aLogger));
            if (aErrorMsg == null) throw new ArgumentNullException(nameof(aErrorMsg));
            aLogger.Error(aErrorMsg.ErrorMessage);
        }

        /// <summary>
        /// Logs an error level log message, creates a Buttplug Error Message with the error description, and returns it.
        /// </summary>
        /// <param name="aLogger">Logger to use for message</param>
        /// <param name="aId">Message ID for created message</param>
        /// <param name="aCode">Error class for message</param>
        /// <param name="aMsg">Error description for message</param>
        /// <returns>A new Error Buttplug message</returns>
        [NotNull]
        public static Error LogErrorMsg([NotNull] this IButtplugLog aLogger, uint aId, Error.ErrorClass aCode, string aMsg)
        {
            if (aLogger == null) throw new ArgumentNullException(nameof(aLogger));
            aLogger.Error(aMsg);
            return new Error(aMsg, aCode, aId);
        }

        /// <summary>
        /// Logs an Error Buttplug message as a warning level log message.
        /// </summary>
        /// <param name="aLogger"></param>
        /// <param name="aErrorMsg"></param>
        public static void LogWarnMsg([NotNull] this IButtplugLog aLogger, [NotNull] Error aErrorMsg)
        {
            if (aLogger == null) throw new ArgumentNullException(nameof(aLogger));
            if (aErrorMsg == null) throw new ArgumentNullException(nameof(aErrorMsg));
            aLogger.Warn(aErrorMsg.ErrorMessage);
        }

        /// <summary>
        /// Logs a warning level log message, creates a Buttplug Error Message with the error description, and returns it.
        /// </summary>
        /// <param name="logger">Logger to use for message</param>
        /// <param name="aId">Message ID for created message</param>
        /// <param name="aCode">Error class for message</param>
        /// <param name="aMsg">Error description for message</param>
        /// <returns>A new Error Buttplug message</returns>
        [NotNull]
        public static Error LogWarnMsg([NotNull] this IButtplugLog logger, uint aId, Error.ErrorClass aCode, string aMsg)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            logger.Warn(aMsg);
            return new Error(aMsg, aCode, aId);
        }
    }
}
