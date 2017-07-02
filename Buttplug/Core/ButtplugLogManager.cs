using System;
using Buttplug.Logging;
using JetBrains.Annotations;

namespace Buttplug.Core
{
    internal class ButtplugLogManager : IButtplugLogManager
    {
        [CanBeNull]
        public event EventHandler<ButtplugLogMessageEventArgs> LogMessageReceived;
        public ButtplugLogLevel Level { private get; set; }

        private void LogMessageHandler([NotNull] object aObject, [NotNull] ButtplugLogMessageEventArgs aMsg)
        {
            if (aMsg.LogMessage.LogLevel <= Level)
            {
                LogMessageReceived?.Invoke(aObject, aMsg);   
            }
        }

        public IButtplugLog GetLogger([NotNull] Type aType)
        {
            // Just pass the type in instead of traversing the stack to find it.
            var logger = new ButtplugLog(LogProvider.GetLogger(aType.Name));
            logger.LogMessageReceived += LogMessageHandler;
            return logger;
        }
    }
}
