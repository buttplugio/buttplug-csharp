using Newtonsoft.Json;

namespace Buttplug.Core.Messages
{
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
        public uint SensorIndex;

        [JsonProperty(Required = Required.Always)]
        public SensorType SensorType;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScalarCmd"/> class.
        /// </summary>
        /// <param name="deviceIndex">Device index.</param>
        /// <param name="scalars">List of per-actuator scalar commands.</param>
        /// <param name="id">Message ID.</param>
        [JsonConstructor]
        public InputCmd(uint deviceIndex, uint sensorIndex, SensorType sensorType, uint id = ButtplugConsts.DefaultMsgId)
            : base(id, deviceIndex)
        {
            SensorIndex = sensorIndex;
            SensorType = sensorType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScalarCmd"/> class.
        /// </summary>
        /// <param name="scalars">List of per-actuator scalar commands.</param>
        public InputCmd(uint sensorIndex, SensorType sensorType)
            : this(uint.MaxValue, sensorIndex, sensorType)
        {
        }
    }
}