using System;

namespace Buttplug.Server.Managers.ETSerialManager
{
    public class ET312HandshakeException : Exception
    {
        public ET312HandshakeException()
        {
        }

        public ET312HandshakeException(string message)
            : base(message)
        {
        }

        public ET312HandshakeException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
