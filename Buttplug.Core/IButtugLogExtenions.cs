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
        /// Logs an Error Buttplug message as an error
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="error">The Error Buttplug message</param>
        public static void LogErrorMsg(this IButtplugLog logger, Error error)
        {
            logger.Error(error.ErrorMessage);
        }

        /// <summary>
        /// Constructs a new Error Buttplug message and logs it as an error
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="aId">The message ID</param>
        /// <param name="aCode">The error class</param>
        /// <param name="aMsg">The error message</param>
        /// <returns>A new Error Buttplug message</returns>
        [NotNull]
        public static Error LogErrorMsg(this IButtplugLog logger, uint aId, Error.ErrorClass aCode, string aMsg)
        {
            logger.Error(aMsg);
            return new Error(aMsg, aCode, aId);
        }

        /// <summary>
        /// Logs an Error Buttplug message as a warning
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="warning"></param>
        public static void LogWarnMsg(this IButtplugLog logger, Error warning)
        {
            logger.Warn(warning.ErrorMessage);
        }

        /// <summary>
        /// Constructs a new Error Buttplug message and logs it as a warning
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="aId">The message ID</param>
        /// <param name="aCode">The error class</param>
        /// <param name="aMsg">The error message</param>
        /// <returns>A new Error Buttplug message</returns>
        [NotNull]
        public static Error LogWarnMsg(this IButtplugLog logger, uint aId, Error.ErrorClass aCode, string aMsg)
        {
            logger.Warn(aMsg);
            return new Error(aMsg, aCode, aId);
        }
    }
}
