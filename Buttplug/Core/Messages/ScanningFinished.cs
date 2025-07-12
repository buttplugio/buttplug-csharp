namespace Buttplug.Core.Messages
{
    /// <summary>
    /// Sent from server when scanning has finished.
    /// </summary>
    [ButtplugMessageMetadata("ScanningFinished")]
    public class ScanningFinished : ButtplugMessage, IButtplugMessageOutgoingOnly
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScanningFinished"/> class. ID is always 0.
        /// </summary>
        public ScanningFinished()
            : base(ButtplugConsts.SystemMsgId)
        {
        }
    }
}