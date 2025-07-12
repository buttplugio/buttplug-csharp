namespace Buttplug.Core.Messages
{
    /// <summary>
    /// Sent to server to request a list of all connected devices.
    /// </summary>
    [ButtplugMessageMetadata("RequestDeviceList")]
    public class RequestDeviceList : ButtplugMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestDeviceList"/> class.
        /// </summary>
        /// <param name="id">Message ID.</param>
        public RequestDeviceList(uint id = ButtplugConsts.DefaultMsgId)
            : base(id)
        {
        }
    }
}