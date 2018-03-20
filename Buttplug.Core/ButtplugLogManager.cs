using System;
using Buttplug.Logging;
using JetBrains.Annotations;

namespace Buttplug.Core
{
    /// <summary>
    /// The Buttplug logging manager.
    /// This handles recieving log messages and reporting any that match the
    /// granularity level to any listeners
    /// </summary>
    public class ButtplugLogManager : IButtplugLogManager
    {
        /// <summary>
        /// This handler is called when a log message has been recieved
        /// </summary>
        [CanBeNull]
        public event EventHandler<ButtplugLogMessageEventArgs> LogMessageReceived;

        /// <summary>
        /// The level of log to report
        /// </summary>
        public ButtplugLogLevel Level { private get; set; }

        private void LogMessageHandler([NotNull] object aObject, [NotNull] ButtplugLogMessageEventArgs aMsg)
        {
            if (aMsg.LogMessage.LogLevel <= Level)
            {
                LogMessageReceived?.Invoke(aObject, aMsg);
            }
        }

        /// <summary>
        /// Gets a Buttplug logger for the specified type
        /// </summary>
        /// <param name="aType">The type this logger is for</param>
        /// <returns>A Buttplug logger</returns>
        public IButtplugLog GetLogger([NotNull] Type aType)
        {
            // Just pass the type in instead of traversing the stack to find it.
            var logger = new ButtplugLog(LogProvider.GetLogger(aType.Name));
            logger.LogMessageReceived += LogMessageHandler;
            return logger;
        }
    }
}
