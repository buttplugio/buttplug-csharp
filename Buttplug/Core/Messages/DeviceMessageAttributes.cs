using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;
using System.Linq;
using Newtonsoft.Json.Converters;

namespace Buttplug.Core.Messages
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ActuatorType 
    {
        [EnumMember(Value = "Vibrate")]
        Vibrate,
        [EnumMember(Value = "Rotate")]
        Rotate,
        [EnumMember(Value = "Oscillate")]
        Oscillate,
        [EnumMember(Value = "Constrict")]
        Constrict,
        [EnumMember(Value = "Inflate")]
        Inflate,
        [EnumMember(Value = "Position")]
        Position
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum SensorType
    {
        [EnumMember(Value = "Battery")]
        Battery,
        [EnumMember(Value = "RSSI")]
        RSSI,
        [EnumMember(Value = "Button")]
        Button,
        [EnumMember(Value = "Pressure")]
        Pressure
    }

    public class GenericDeviceMessageAttributes 
    {
        [JsonIgnore]
        public uint Index { get { return _index; } }

        [JsonIgnore]
        internal uint _index;
        [JsonProperty(Required = Required.Always)]
        public readonly string FeatureDescriptor;
        [JsonProperty(Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public readonly ActuatorType ActuatorType;
        [JsonProperty(Required = Required.Always)]
        public readonly uint StepCount;
    }

    public class SensorDeviceMessageAttributes
    {
        [JsonIgnore]
        public uint Index { get { return _index; } }

        [JsonIgnore]
        internal uint _index;
        [JsonProperty(Required = Required.Always)]
        public readonly string FeatureDescriptor;
        [JsonProperty(Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public readonly SensorType SensorType;
        [JsonProperty(Required = Required.Always)]
        public readonly uint[][] SensorRange;
    }

    public class RawDeviceMessageAttributes
    {
        public readonly string[] Endpoints;
    }

    public class NullDeviceMessageAttributes 
    {
    }

    public class DeviceMessageAttributes
    {
        public GenericDeviceMessageAttributes[] ScalarCmd;
        public GenericDeviceMessageAttributes[] RotateCmd;
        public GenericDeviceMessageAttributes[] LinearCmd;
        public SensorDeviceMessageAttributes[] SensorReadCmd;
        public SensorDeviceMessageAttributes[] SensorSubscribeCmd;

        public readonly RawDeviceMessageAttributes[] RawReadCmd;
        public readonly RawDeviceMessageAttributes[] RawWriteCmd;
        public readonly RawDeviceMessageAttributes[] RawSubscribeCmd;

        public readonly NullDeviceMessageAttributes StopDeviceCmd;

        // Set Indexes for all attributes
        //
        // This is a hack to live until when we actually transfer ids as part of buttplug messages,
        // which will probably be in spec v4.
        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            ScalarCmd?.Select((x, i) => (x, i)).ToList().ForEach(x => x.x._index = (uint)x.i);
            RotateCmd?.Select((x, i) => (x, i)).ToList().ForEach(x => x.x._index = (uint)x.i);
            LinearCmd?.Select((x, i) => (x, i)).ToList().ForEach(x => x.x._index = (uint)x.i);
            SensorReadCmd?.Select((x, i) => (x, i)).ToList().ForEach(x => x.x._index = (uint)x.i);
            SensorSubscribeCmd?.Select((x, i) => (x, i)).ToList().ForEach(x => x.x._index = (uint)x.i);
        }
    }
}
