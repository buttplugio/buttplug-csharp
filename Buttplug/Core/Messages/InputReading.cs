using System.Collections.Generic;
using Newtonsoft.Json;

namespace Buttplug.Core.Messages
{
    
    public class InputReadingDataValue<T>
    {
        public T Value;
    }

    public class InputReadingData
    {
        public InputReadingDataValue<uint> Battery;
        public InputReadingDataValue<int> Rssi;
        public InputReadingDataValue<uint> Button;
        public InputReadingDataValue<uint> Pressure;
    }
    
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
        public readonly uint FeatureIndex;

        [JsonProperty(Required = Required.Always)]
        public readonly InputReadingData Data;
    }
}