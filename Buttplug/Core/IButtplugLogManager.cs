using System;

namespace Buttplug.Core
{
    public interface IButtplugLogManager
    {
        event EventHandler<ButtplugLogMessageEventArgs> LogMessageReceived;
        IButtplugLog GetLogger(Type aType);
        ButtplugLogLevel Level { get; set; }
    }
}
