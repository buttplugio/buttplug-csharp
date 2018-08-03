using System;
using Buttplug.Core;

namespace Buttplug.Client
{
    public class ButtplugClientException : Exception
    {
        public readonly ButtplugMessage Message;

        public ButtplugClientException(ButtplugMessage aMsg)
        {
            Message = aMsg;
        }
    }
}