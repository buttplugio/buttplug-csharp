using System;
using System.Runtime.Serialization;

namespace Buttplug.Server.Managers.ArduinoManager
{
    [Serializable]
    internal class ArduinoDeviceException : Exception
    {
        public ArduinoDeviceException()
        {
        }

        public ArduinoDeviceException(string message) : base(message)
        {
        }

        public ArduinoDeviceException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ArduinoDeviceException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}