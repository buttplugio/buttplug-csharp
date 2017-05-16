namespace Buttplug.Core
{
    public class MessageReceivedEventArgs
    {
        public MessageReceivedEventArgs(ButtplugMessage message)
        {
            Message = message;
        }

        public ButtplugMessage Message { get; }
    }
}