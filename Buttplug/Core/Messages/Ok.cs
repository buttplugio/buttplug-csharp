namespace Buttplug.Core.Messages
{
    /// <summary>
    /// Signifies the success of the last message/query.
    /// </summary>
    [ButtplugMessageMetadata("Ok")]
    public class Ok : ButtplugMessage, IButtplugMessageOutgoingOnly
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Ok"/> class.
        /// </summary>
        /// <param name="id">Message ID. Should match the ID of the message being responded to.</param>
        public Ok(uint id)
            : base(id)
        {
        }
    }
}