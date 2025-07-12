namespace Buttplug.Core.Messages
{
    /// <summary>
    /// Sent to server, stops actions of a specific device.
    /// </summary>
    [ButtplugMessageMetadata("StopDeviceCmd")]
    public class StopDeviceCmd : ButtplugDeviceMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StopDeviceCmd"/> class.
        /// </summary>
        /// <param name="deviceIndex">Device index.</param>
        /// <param name="id">Message ID.</param>
        public StopDeviceCmd(uint deviceIndex = uint.MaxValue, uint id = ButtplugConsts.DefaultMsgId)
            : base(id, deviceIndex)
        {
        }
    }
}