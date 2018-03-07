using System;
using System.Collections.Generic;
using Buttplug.Core.Messages;

namespace Buttplug.Components.Controls
{
    public class ButtplugDeviceInfo
    {
        public string Name { get; }

        public uint Index { get; }

        public Dictionary<string, MessageAttributes> Messages { get; }

        public ButtplugDeviceInfo(uint aIndex, string aName,
            Dictionary<string, MessageAttributes> aMessages)
        {
            Index = aIndex;
            Name = aName;
            Messages = aMessages;
        }

        public override string ToString()
        {
            return $"{Index}: {Name}";
        }

        public bool SupportsMessage(Type aMsg)
        {
            return Messages.ContainsKey(aMsg.Name);
        }
    }
}