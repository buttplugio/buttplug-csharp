using System.Collections.Generic;
using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace Buttplug.Client
{
    /// <summary>
    /// The Buttplug Client representation of a Buttplug Device
    /// </summary>
    public class ButtplugClientDevice
    {
        /// <summary>
        /// The device index
        /// If a device is removed, this may be the only populated field
        /// If the same device reconnects, the index should be reused
        /// </summary>
        public readonly uint Index;

        /// <summary>
        /// The device name
        /// </summary>
        [NotNull]
        public readonly string Name;

        /// <summary>
        /// The messages supported by this device, with additional attributes
        /// </summary>
        [NotNull]
        public readonly Dictionary<string, MessageAttributes> AllowedMessages;

        /// <summary>
        /// Initializes a new instance of the <see cref="ButtplugClientDevice"/> class.
        /// Constructs from a DeviceMessageInfo element of a DeviceList message
        /// </summary>
        /// <param name="aDevInfo">A DeviceMessageInfo element of a Buttplug DeviceList message</param>
        public ButtplugClientDevice(DeviceMessageInfo aDevInfo)
        {
            Index = aDevInfo.DeviceIndex;
            Name = aDevInfo.DeviceName;
            AllowedMessages = new Dictionary<string, MessageAttributes>(aDevInfo.DeviceMessages);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ButtplugClientDevice"/> class.
        /// Consructs a device from scratch
        /// </summary>
        /// <param name="aIndex">The device index</param>
        /// <param name="aName">The device name</param>
        /// <param name="aMessages">The supported messages</param>
        public ButtplugClientDevice(uint aIndex, string aName, Dictionary<string, MessageAttributes> aMessages)
        {
            Index = aIndex;
            Name = aName;
            AllowedMessages = new Dictionary<string, MessageAttributes>(aMessages);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ButtplugClientDevice"/> class.
        /// Constructs from a DeviceAdded message
        /// </summary>
        /// <param name="aDevInfo">A Buttplug DeviceAdded message</param>
        public ButtplugClientDevice(DeviceAdded aDevInfo)
        {
            Index = aDevInfo.DeviceIndex;
            Name = aDevInfo.DeviceName;
            AllowedMessages = new Dictionary<string, MessageAttributes>(aDevInfo.DeviceMessages);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ButtplugClientDevice"/> class.
        /// Constructs from a DeviceRemoved message (no name or supported messages)
        /// </summary>
        /// <param name="aDevInfo">A Buttplug DeviceRemoved message</param>
        public ButtplugClientDevice(DeviceRemoved aDevInfo)
        {
            Index = aDevInfo.DeviceIndex;
            Name = string.Empty;
            AllowedMessages = new Dictionary<string, MessageAttributes>();
        }
    }
}