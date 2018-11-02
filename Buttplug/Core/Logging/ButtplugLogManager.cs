using System;
using Buttplug.Logging;
using JetBrains.Annotations;

namespace Buttplug.Core.Logging
{
    /// <inheritdoc cref="IButtplugLogManager"/>
    // ReSharper disable once InheritdocConsiderUsage
    public class ButtplugLogManager : IButtplugLogManager
    {
        /// <inheritdoc cref="IButtplugLogManager"/>>
        [CanBeNull]
        public event EventHandler<ButtplugLogMessageEventArgs> LogMessageReceived;

        /// <inheritdoc cref="IButtplugLogManager"/>>
        public ButtplugLogLevel Level { get; set; }

        private void LogMessageHandler([NotNull] object aObject, [NotNull] ButtplugLogMessageEventArgs aMsg)
        {
            if (aObject == null) throw new ArgumentNullException(nameof(aObject));
            if (aMsg == null) throw new ArgumentNullException(nameof(aMsg));
            if (aMsg.LogMessage.LogLevel <= Level)
            {
                LogMessageReceived?.Invoke(aObject, aMsg);
            }
        }

        /// <inheritdoc cref="IButtplugLogManager"/>
        public IButtplugLog GetLogger([NotNull] Type aType)
        {
            if (aType == null) throw new ArgumentNullException(nameof(aType));
            // Just pass the type in instead of traversing the stack to find it.
            var logger = new ButtplugLog(LogProvider.GetLogger(aType.Name));
            logger.LogMessageReceived += LogMessageHandler;
            return logger;
        }
    }
}
