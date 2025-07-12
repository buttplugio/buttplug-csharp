namespace Buttplug.Core.Messages
{
    /// <summary>
    /// Sent to server, to start scanning for devices across supported busses.
    /// </summary>
    [ButtplugMessageMetadata("StartScanning")]
    public class StartScanning : ButtplugMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StartScanning"/> class.
        /// </summary>
        /// <param name="id">Message ID.</param>
        public StartScanning(uint id = ButtplugConsts.DefaultMsgId)
            : base(id)
        {
        }
    }
}