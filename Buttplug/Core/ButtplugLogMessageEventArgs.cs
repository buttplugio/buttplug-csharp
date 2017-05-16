using System;
using Buttplug.Messages;

namespace Buttplug.Core
{
    public class ButtplugLogMessageEventArgs : EventArgs
    {
        public Log LogMessage { get; }

        public ButtplugLogMessageEventArgs(string aLogLevel, string aMessage)
        {
            LogMessage = new Log(aLogLevel, aMessage);
        }
    }
}
