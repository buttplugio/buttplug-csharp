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
        public readonly List<string> AllowedMessages;

        public ButtplugClientDevice(DeviceMessageInfo aDevInfo)
        {
            Index = aDevInfo.DeviceIndex;
            Name = aDevInfo.DeviceName;
            AllowedMessages = new List<string>(aDevInfo.DeviceMessages);
        }

        public ButtplugClientDevice(DeviceAdded aDevInfo)
        {
            Index = aDevInfo.DeviceIndex;
            Name = aDevInfo.DeviceName;
            AllowedMessages = new List<string>(aDevInfo.DeviceMessages);
        }

        public ButtplugClientDevice(DeviceRemoved aDevInfo)
        {
            Index = aDevInfo.DeviceIndex;
            Name = string.Empty;
            AllowedMessages = new List<string>();
        }
    }
}