using System;
using Buttplug.Messages;
using JetBrains.Annotations;

namespace Buttplug.Core
{
    public class ButtplugLogMessageEventArgs : EventArgs
    {
        [NotNull]
        public Log LogMessage { get; }

        public ButtplugLogMessageEventArgs(ButtplugLogLevel aLogLevel, string aMessage)
        {
            LogMessage = new Log(aLogLevel, aMessage);
        }
    }
}
