using System;

namespace Buttplug.Server.Managers.ETSerialManager
{
    public class ET312CommunicationException : Exception
    {
        public ET312CommunicationException()
        {
        }

        public ET312CommunicationException(string message)
            : base(message)
        {
        }

        public ET312CommunicationException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
