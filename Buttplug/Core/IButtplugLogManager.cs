using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Buttplug.Core
{
    public interface IButtplugLogManager
    {
        event EventHandler<ButtplugLogMessageEventArgs> LogMessageReceived;
        IButtplugLog GetLogger(Type aType);
        ButtplugLogLevel Level { get; set; }
    }
}
