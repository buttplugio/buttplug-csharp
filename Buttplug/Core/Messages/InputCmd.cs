using Newtonsoft.Json;

namespace Buttplug.Core.Messages
{
    public enum InputType
    {
        Battery,
        Rssi,
        Button,
        Pressure,
    }

    public enum InputCommandType
    {
        Read,
        Subscribe,
        Unsubscribe,
    }
    
    /// <summary>
    /// Sent to server, generic message that can control any device that takes a single value and staticly
    /// sets an actuator to that value (speed, oscillation frequency, instanteous position, etc).
    /// </summary>
    [ButtplugMessageMetadata("SensorReadCmd")]
    public class InputCmd : ButtplugDeviceMessage
    {
        /// <summary>
        /// List of vibrator speeds.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public uint FeatureIndex;

        [JsonProperty(Required = Required.Always)]
        public InputType InputType;
        
        [JsonProperty(Required = Required.Always)]
        public InputCommandType InputCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="InputCmd"/> class.
        /// </summary>
        /// <param name="deviceIndex">Device index.</param>
        /// <param name="featureIndex">Feature index.</param>
        /// <param name="inputType">Type of input</param>
        /// <param name="id">Message ID.</param>
        [JsonConstructor]
        public InputCmd(uint deviceIndex, uint featureIndex, InputType inputType, InputCommandType inputCommand, uint id = ButtplugConsts.DefaultMsgId)
            : base(id, deviceIndex)
        {
            FeatureIndex = featureIndex;
            InputType = inputType;
            InputCommand = inputCommand;
        }
    }
}