using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace Buttplug.Client
{
    public class ErrorEventArgs
    {
        [NotNull]
        public readonly Error Message;

        public ErrorEventArgs(Error aMsg)
        {
            Message = aMsg;
        }
    }
}