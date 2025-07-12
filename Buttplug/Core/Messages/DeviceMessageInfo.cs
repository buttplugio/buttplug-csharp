using Newtonsoft.Json;

namespace Buttplug.Core.Messages
{
    /// <summary>
    /// Container class for describing a device.
    /// </summary>
    public class DeviceMessageInfo : IButtplugDeviceInfoMessage
    {
        /// <summary>
        /// Name of the device.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string DeviceName;

        /// <summary>
        /// Device index.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly uint DeviceIndex;

        /// <summary>
        /// Device display name, set up by the user.
        /// </summary>
        public readonly string DeviceDisplayName;

        /// <summary>
        /// Recommended amount of time between commands, in milliseconds.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public readonly uint DeviceMessageTimingGap;

        /// <summary>
        /// List of messages that a device supports, with additional attribute data.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly DeviceMessageAttributes DeviceMessages;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceMessageInfo"/> class.
        /// </summary>
        /// <param name="index">Device index.</param>
        /// <param name="name">Device name.</param>
        /// <param name="messages">List of device messages/attributes supported.</param>
        public DeviceMessageInfo(uint index, string name,
            DeviceMessageAttributes messages)
        {
            DeviceName = name;
            DeviceIndex = index;
            DeviceMessages = messages;
        }

        // Implementation details for IButtplugDeviceInfoMessage interface
        string IButtplugDeviceInfoMessage.DeviceName => DeviceName;

        uint IButtplugDeviceInfoMessage.DeviceIndex => DeviceIndex;

        DeviceMessageAttributes IButtplugDeviceInfoMessage.DeviceMessages => DeviceMessages;

        string IButtplugDeviceInfoMessage.DeviceDisplayName => DeviceDisplayName;

        uint IButtplugDeviceInfoMessage.DeviceMessageTimingGap => DeviceMessageTimingGap;
    }
}