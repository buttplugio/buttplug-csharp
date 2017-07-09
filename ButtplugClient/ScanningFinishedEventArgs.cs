using Buttplug.Messages;
using JetBrains.Annotations;

namespace Buttplug.Client
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