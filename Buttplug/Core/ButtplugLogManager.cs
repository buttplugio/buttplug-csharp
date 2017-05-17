using System;
using System.Collections.Generic;
using Buttplug.Logging;
namespace Buttplug.Core
{
    public class ButtplugLogManager
    {
        public event EventHandler<ButtplugLogMessageEventArgs> LogMessageReceived;
        private string _level = "Off";

        public string Level
        {
            get => _level;
            set
            {
                if (value is null || !Levels.Contains(value))
                {
                    throw new ArgumentException("Invalid Log Level");
                }
                _level = value;
            }
        }

        public static readonly List<string> Levels = new List<string>()
        {
            "Off",
            "Fatal",
            "Error",
            "Warn",
            "Info",
            "Debug",
            "Trace"
        };

        private void LogMessageHandler(object o, ButtplugLogMessageEventArgs m)
        {
            if (Levels.IndexOf(m.LogMessage.LogLevel) <= Levels.IndexOf(_level))
            {
                LogMessageReceived?.Invoke(o, m);   
            }
        }

        public ButtplugLog GetLogger(ILog aLogger)
        {
            var logger = new ButtplugLog(aLogger);
            logger.LogMessageReceived += LogMessageHandler;
            return logger;
        }
    }
}
