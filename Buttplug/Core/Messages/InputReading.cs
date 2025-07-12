using System.Collections.Generic;
using Newtonsoft.Json;

namespace Buttplug.Core.Messages
{
    /// <summary>
    /// Sent to server, generic message that can control any device that takes a single value and staticly
    /// sets an actuator to that value (speed, oscillation frequency, instanteous position, etc).
    /// </summary>
    [ButtplugMessageMetadata("SensorReading")]
    public class InputReading : ButtplugDeviceMessage
    {
        /// <summary>
        /// List of vibrator speeds.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly uint SensorIndex;

        [JsonProperty(Required = Required.Always)]
        public readonly SensorType SensorType;

        [JsonProperty(Required = Required.Always)]
        public readonly List<int> data;
    }
}