using System;
using System.Collections.Generic;
using Buttplug.Logging;
namespace Buttplug.Core
{
    internal class ButtplugLogManager : IButtplugLogManager
    {
        public event EventHandler<ButtplugLogMessageEventArgs> LogMessageReceived;

        public ButtplugLogLevel Level { get; set; }

        private void LogMessageHandler(object o, ButtplugLogMessageEventArgs m)
        {
            if (m.LogMessage.LogLevel <= Level)
            {
                LogMessageReceived?.Invoke(o, m);   
            }
        }

        public IButtplugLog GetLogger(Type aType)
        {
            // Just pass the type in instead of traversing the stack to find it.
            var logger = new ButtplugLog(LogProvider.GetLogger(aType.Name));
            logger.LogMessageReceived += LogMessageHandler;
            return logger;
        }
    }
}
