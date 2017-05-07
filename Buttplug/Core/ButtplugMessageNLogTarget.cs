using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buttplug.Messages;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace Buttplug.Core
{
    [Target("ButtplugLogger")]
    public sealed class ButtplugMessageNLogTarget : TargetWithLayout
    {
        public class NLogMessageEventArgs : EventArgs
        {
            public Log LogMessage { get; }
            public NLogMessageEventArgs(LogEventInfo aLogEvent)
            {
                LogMessage = new Log(aLogEvent.Level.Name, aLogEvent.Message);
            }
        }

        public event EventHandler<NLogMessageEventArgs> LogMessageReceived;

        protected override void Write(LogEventInfo aLogEvent)
        {
            LogMessageReceived?.Invoke(this, new NLogMessageEventArgs(aLogEvent));
        }
    }
}
