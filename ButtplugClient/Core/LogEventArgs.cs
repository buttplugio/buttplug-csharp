using Buttplug.Messages;
using JetBrains.Annotations;

namespace ButtplugClient.Core
{
    public class LogEventArgs
    {
        [NotNull]
        public readonly Log Message;

        public LogEventArgs(Log aMsg)
        {
            Message = aMsg;
        }
    }
}