using System;
using Buttplug.Core;

namespace Buttplug.Client
{
    public class ButtplugClientException : Exception
    {
        public readonly ButtplugMessage ButtplugErrorMessage;

        public ButtplugClientException(ButtplugMessage aMsg)
        {
            ButtplugErrorMessage = aMsg;
        }
    }
}