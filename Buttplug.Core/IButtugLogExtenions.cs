using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace Buttplug.Core
{
    /// <summary>
    /// Extension methods for <see cref="IButtplugLog"/>.
    /// </summary>
    public static class ButtugLogExtenions
    {
        /// <summary>
        /// Logs an <see cref="Error"/> Buttplug message as an error level log message.
        /// </summary>
        /// <param name="logger">Logger to use for message</param>
        /// <param name="error">Error Buttplug message</param>
        public static void LogErrorMsg(this IButtplugLog logger, Error error)
        {
            logger.Error(error.ErrorMessage);
        }

        /// <summary>
        /// Logs an error level log message, creates a Buttplug Error Message with the error description, and returns it.
        /// </summary>
        /// <param name="logger">Logger to use for message</param>
        /// <param name="aId">Message ID for created message</param>
        /// <param name="aCode">Error class for message</param>
        /// <param name="aMsg">Error description for message</param>
        /// <returns>A new Error Buttplug message</returns>
        [NotNull]
        public static Error LogErrorMsg(this IButtplugLog logger, uint aId, Error.ErrorClass aCode, string aMsg)
        {
            logger.Error(aMsg);
            return new Error(aMsg, aCode, aId);
        }

        /// <summary>
        /// Logs an Error Buttplug message as a warning level log message.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="warning"></param>
        public static void LogWarnMsg(this IButtplugLog logger, Error warning)
        {
            logger.Warn(warning.ErrorMessage);
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
        public static Error LogWarnMsg(this IButtplugLog logger, uint aId, Error.ErrorClass aCode, string aMsg)
        {
            logger.Warn(aMsg);
            return new Error(aMsg, aCode, aId);
        }
    }
}
