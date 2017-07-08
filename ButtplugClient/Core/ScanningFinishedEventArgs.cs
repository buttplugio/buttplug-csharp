using Buttplug.Messages;
using JetBrains.Annotations;

namespace ButtplugClient.Core
{
    public class ScanningFinishedEventArgs
    {
        [NotNull]
        public readonly ScanningFinished Message;

        public ScanningFinishedEventArgs(ScanningFinished aMsg)
        {
            Message = aMsg;
        }
    }
}