using System;
using Buttplug.Logging;
using JetBrains.Annotations;

namespace Buttplug.Core
{
    /// <summary>
    /// Handles receiving log messages and reporting any that match requested granuarlity levels
    /// to be sent to clients.
    /// </summary>
    public class ButtplugLogManager : IButtplugLogManager
    {
        /// <summary>
        /// Called when a log message has been received.
        /// </summary>
        [CanBeNull]
        public event EventHandler<ButtplugLogMessageEventArgs> LogMessageReceived;

        /// <summary>
        /// Log level to report and store.
        /// </summary>
        public ButtplugLogLevel Level { private get; set; }

        private void LogMessageHandler([NotNull] object aObject, [NotNull] ButtplugLogMessageEventArgs aMsg)
        {
            if (aMsg.LogMessage.LogLevel <= Level)
            {
                LogMessageReceived?.Invoke(aObject, aMsg);
            }
        }

        /// <inheritdoc cref="IButtplugLogManager"/>
        public IButtplugLog GetLogger([NotNull] Type aType)
        {
            // Just pass the type in instead of traversing the stack to find it.
            var logger = new ButtplugLog(LogProvider.GetLogger(aType.Name));
            logger.LogMessageReceived += LogMessageHandler;
            return logger;
        }
    }
}
