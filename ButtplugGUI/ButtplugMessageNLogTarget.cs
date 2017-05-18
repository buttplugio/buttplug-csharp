using System;
using Buttplug.Core;
using Buttplug.Messages;
using NLog;
using NLog.Targets;

namespace ButtplugGUI
{
    [Target("ButtplugLogger")]
    public sealed class ButtplugMessageNLogTarget : TargetWithLayout
    {
        public class NLogMessageEventArgs : EventArgs
        {
            public Log LogMessage { get; }

            public NLogMessageEventArgs(LogEventInfo aLogEvent)
            {
                ButtplugLogLevel level;
                if (!Enum.TryParse(aLogEvent.Level.ToString(), out level))
                {
                    throw new ArgumentException("Cannot parse log level");
                }
                LogMessage = new Log(level, aLogEvent.Message);
            }
        }

        public event EventHandler<NLogMessageEventArgs> LogMessageReceived;

        protected override void Write(LogEventInfo aLogEvent)
        {
            LogMessageReceived?.Invoke(this, new NLogMessageEventArgs(aLogEvent));
        }
    }
}