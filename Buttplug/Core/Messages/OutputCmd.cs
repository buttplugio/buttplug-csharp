using Newtonsoft.Json;

namespace Buttplug.Core.Messages
{
    public enum OutputType
    {
        Vibrate,
        Rotate,
        Oscillate,
        Constrict,
        Spray,
        Heater,
        Led,
        PositionWithDuration,
        RotateWithDirection
    }

    public class OutputCommand
    {
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public OutputCommandValue Vibrate;
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public OutputCommandValue Rotate;
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public OutputCommandValue Oscillate;
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public OutputCommandValue Constrict;
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public OutputCommandValue Spray;
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public OutputCommandValue Heater;
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public OutputCommandValue Led;
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public OutputCommandPositionWithDuration PositionWithDuration;
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public OutputCommandRotationWithDirection RotationWithDirection;
    }

    public class OutputCommandValue
    {
        [JsonProperty(Required = Required.Always)]
        public uint Value;

        internal OutputCommandValue(uint value)
        {
            Value = value;
        }
    }

    public class OutputCommandPositionWithDuration
    {
        [JsonProperty(Required = Required.Always)]
        public uint Position;
        [JsonProperty(Required = Required.Always)]
        public uint Duration;
    }

    public class OutputCommandRotationWithDirection
    {
        [JsonProperty(Required = Required.Always)]
        public uint Speed;
        [JsonProperty(Required = Required.Always)]
        public bool Clockwise;
    }
    
    [ButtplugMessageMetadata("OutputCmd")]
    public class OutputCmd: ButtplugDeviceMessage
    {
        
        [JsonProperty(Required = Required.Always)]
        public uint FeatureIndex;

        [JsonProperty(Required = Required.Always)]
        public OutputCommand Command;

        [JsonConstructor]
        public OutputCmd(uint deviceIndex, uint featureIndex, OutputCommand command, uint id = ButtplugConsts.DefaultMsgId)
            : base(id, deviceIndex)
        {
            FeatureIndex = featureIndex;
            Command = command;
        }
    }
}