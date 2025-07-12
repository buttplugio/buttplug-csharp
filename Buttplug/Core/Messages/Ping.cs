namespace Buttplug.Core.Messages
{
    /// <summary>
    /// Sent to server, at an interval specified by the server. If ping is not received in a timely
    /// manner, devices are stopped and client/server connection is severed.
    /// </summary>
    // Resharper doesn't seem to be able to deduce that though.
    // ReSharper disable once ClassNeverInstantiated.Global
    [ButtplugMessageMetadata("Ping")]
    public class Ping : ButtplugMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Ping"/> class.
        /// </summary>
        /// <param name="id">Message ID.</param>
        public Ping(uint id = ButtplugConsts.DefaultMsgId)
            : base(id)
        {
        }
    }
}