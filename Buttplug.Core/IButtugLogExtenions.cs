using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace Buttplug.Core
{
    /// <summary>
    /// Extension methods for <see cref="IButtplugLog"/>.
    /// </summary>
    public static class IButtugLogExtenions
    {
        public static void LogErrorMsg(this IButtplugLog logger, Error error)
        {
            logger.Error(error.ErrorMessage, false);
        }

        [NotNull]
        public static Error LogErrorMsg(this IButtplugLog logger, uint aId, Error.ErrorClass aCode, string aMsg)
        {
            logger.Error(aMsg, false);
            return new Error(aMsg, aCode, aId);
        }

        public static void LogWarnMsg(this IButtplugLog logger, Error warning)
        {
            logger.Warn(warning.ErrorMessage, false);
        }

        [NotNull]
        public static Error LogWarnMsg(this IButtplugLog logger, uint aId, Error.ErrorClass aCode, string aMsg)
        {
            logger.Warn(aMsg, false);
            return new Error(aMsg, aCode, aId);
        }
    }
}
