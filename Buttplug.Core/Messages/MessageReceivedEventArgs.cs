using System;

namespace Buttplug.Core
{
    /// <summary>
    /// Event fired when a new <see cref="ButtplugMessage"/> is received.
    /// </summary>
    public class MessageReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Buttplug message that was received.
        /// </summary>
        public ButtplugMessage Message { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="aMessage">Buttplug message that was received</param>
        public MessageReceivedEventArgs(ButtplugMessage aMessage)
        {
            Message = aMessage;
        }
    }
}