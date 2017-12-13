using System.Collections.Generic;
using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace Buttplug.Client
{
    public class ButtplugClientDevice
    {
        [NotNull]
        public readonly uint Index;

        [NotNull]
        public readonly string Name;

        [NotNull]
        public readonly Dictionary<string, MessageAttributes> AllowedMessages;

        public ButtplugClientDevice(DeviceMessageInfo aDevInfo)
        {
            Index = aDevInfo.DeviceIndex;
            Name = aDevInfo.DeviceName;
            AllowedMessages = new Dictionary<string, MessageAttributes>(aDevInfo.DeviceMessages);
        }

        public ButtplugClientDevice(uint aIndex, string aName, Dictionary<string, MessageAttributes> aMessages)
        {
            Index = aIndex;
            Name = aName;
            AllowedMessages = new Dictionary<string, MessageAttributes>(aMessages);
        }

        public ButtplugClientDevice(DeviceAdded aDevInfo)
        {
            Index = aDevInfo.DeviceIndex;
            Name = aDevInfo.DeviceName;
            AllowedMessages = new Dictionary<string, MessageAttributes>(aDevInfo.DeviceMessages);
        }

        public ButtplugClientDevice(DeviceRemoved aDevInfo)
        {
            Index = aDevInfo.DeviceIndex;
            Name = string.Empty;
            AllowedMessages = new Dictionary<string, MessageAttributes>();
        }
    }
}