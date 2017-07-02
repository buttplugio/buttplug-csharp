using System;

namespace Buttplug.Core
{
    public class MessageReceivedEventArgs : EventArgs
    {
        public MessageReceivedEventArgs(ButtplugMessage aMessage)
        {
            Message = aMessage;
        }

        public ButtplugMessage Message { get; }
    }
}