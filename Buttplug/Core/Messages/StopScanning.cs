namespace Buttplug.Core.Messages
{
    /// <summary>
    /// Sent to server, to stop scanning for devices across supported busses.
    /// </summary>
    [ButtplugMessageMetadata("StopScanning")]
    public class StopScanning : ButtplugMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StopScanning"/> class.
        /// </summary>
        /// <param name="id">Message ID.</param>
        public StopScanning(uint id = ButtplugConsts.DefaultMsgId)
            : base(id)
        {
        }
    }
}