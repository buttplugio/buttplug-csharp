using System;

namespace Buttplug.Core
{
    /// <summary>
    /// An event encapsulating a recieved Buttplug message
    /// </summary>
    public class MessageReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the received Buttplug message
        /// </summary>
        public ButtplugMessage Message { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="aMessage">The recieved Buttplug message</param>
        public MessageReceivedEventArgs(ButtplugMessage aMessage)
        {
            Message = aMessage;
        }
    }
}