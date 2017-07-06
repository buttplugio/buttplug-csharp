using System;
using JetBrains.Annotations;

namespace Buttplug.Core
{
    public interface IButtplugLogManager
    {
        [CanBeNull]
        event EventHandler<ButtplugLogMessageEventArgs> LogMessageReceived;

        [NotNull]
        IButtplugLog GetLogger(Type aType);

        ButtplugLogLevel Level { set; }
    }
}
