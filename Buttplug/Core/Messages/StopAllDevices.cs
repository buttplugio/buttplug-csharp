namespace Buttplug.Core.Messages
{
    /// <summary>
    /// Sent to server, stops actions of all currently connected devices.
    /// </summary>
    [ButtplugMessageMetadata("StopAllDevices")]
    public class StopAllDevices : ButtplugMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StopAllDevices"/> class.
        /// </summary>
        /// <param name="id">Message ID.</param>
        public StopAllDevices(uint id = ButtplugConsts.DefaultMsgId)
            : base(id)
        {
        }
    }
}