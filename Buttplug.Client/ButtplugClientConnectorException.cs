using System;

namespace Buttplug.Client
{
    public class ButtplugClientConnectorException : Exception
    {
        public ButtplugClientConnectorException()
        {
        }

        public ButtplugClientConnectorException(string message, Exception e = null)
           : base(message, e)
        {
        }
    }
}