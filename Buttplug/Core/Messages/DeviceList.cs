using System.Collections.Generic;
using Newtonsoft.Json;

namespace Buttplug.Core.Messages
{
    public enum FeatureType
    {
        Vibrate,
        Rotate,
        Oscillate,
        Constrict,
        Spray,
        Heater,
        Led,
        PositionWithDuration,
        RotateWithDirection,        
        Battery,
        Rssi,
        Button,
        Pressure,
    }
    public class DeviceFeatureInput
    {
        public readonly List<List<uint>> ValueRange;
        public readonly List<InputCommandType> InputCommands;
    }

    public class DeviceFeatureOutput
    {
        public readonly uint StepCount;
    }
    
    public class DeviceFeature
    {
        public readonly uint FeatureIndex;
        public readonly string FeatureDescription;
        public readonly FeatureType FeatureType;
        public readonly Dictionary<OutputType, DeviceFeatureOutput> Output = new Dictionary<OutputType, DeviceFeatureOutput>();
        public readonly Dictionary<InputType, DeviceFeatureInput> Input = new Dictionary<InputType, DeviceFeatureInput>();
    }
    
    /// <summary>
    /// Container class for describing a device.
    /// </summary>
    public class DeviceMessageInfo
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
        [JsonProperty(Required = Required.Default)]
        public readonly string DeviceDisplayName;

        /// <summary>
        /// Recommended amount of time between commands, in milliseconds.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly uint DeviceMessageTimingGap;

        /// <summary>
        /// List of messages that a device supports, with additional attribute data.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly Dictionary<uint, DeviceFeature> DeviceFeatures;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceMessageInfo"/> class.
        /// </summary>
        /// <param name="index">Device index.</param>
        /// <param name="name">Device name.</param>
        /// <param name="messages">List of device messages/attributes supported.</param>
        public DeviceMessageInfo(uint index, string name,
            Dictionary<uint, DeviceFeature> features)
        {
            DeviceName = name;
            DeviceIndex = index;
            DeviceFeatures = features;
        }
    }
    
    /// <summary>
    /// List of devices connected to the server.
    /// </summary>
    [ButtplugMessageMetadata("DeviceList")]
    public class DeviceList : ButtplugMessage, IButtplugMessageOutgoingOnly
    {
        /// <summary>
        /// List of devices currently connected.
        /// </summary>
        [JsonProperty(Required = Required.Always, NullValueHandling = NullValueHandling.Ignore)]
        public readonly Dictionary<uint, DeviceMessageInfo> Devices;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceList"/> class.
        /// </summary>
        /// <param name="deviceList">List of devices currently connected.</param>
        /// <param name="id">Message ID.</param>
        public DeviceList(Dictionary<uint, DeviceMessageInfo> deviceList, uint id)
            : base(id)
        {
            Devices = deviceList;
        }

        /// <inheritdoc />
        internal DeviceList()
            : base(0)
        {
        }
    }
}