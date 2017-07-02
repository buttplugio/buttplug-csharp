using System;

namespace Buttplug.Core
{
    public class MessageReceivedEventArgs : EventArgs
    {
        public MessageReceivedEventArgs(ButtplugMessage message)
        {
            Message = message;
        }

        public ButtplugMessage Message { get; }
    }
}