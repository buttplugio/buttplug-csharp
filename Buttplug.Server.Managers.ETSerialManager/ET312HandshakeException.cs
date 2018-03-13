using System;

namespace Buttplug.Server.Managers.ETSerialManager
{
    // ReSharper disable once InconsistentNaming
    public class ET312HandshakeException : Exception
    {
        public ET312HandshakeException(string message)
            : base(message)
        {
        }
    }
}
