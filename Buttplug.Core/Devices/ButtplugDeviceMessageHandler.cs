using System;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Messages;

namespace Buttplug.Core.Devices
{
    /// <summary>
    /// A container class for message functions and attributes
    /// </summary>
    public class ButtplugDeviceMessageHandler
    {
        /// <summary>
        /// The function to call when a message of the particular type is received
        /// </summary>
        public Func<ButtplugDeviceMessage, CancellationToken, Task<ButtplugMessage>> Function;

        /// <summary>
        /// A list of attributes ascoiated with the message
        /// </summary>
        public MessageAttributes Attrs;

        /// <summary>
        /// Initializes a new instance of the <see cref="ButtplugDeviceMessageHandler"/> class.
        /// </summary>
        /// <param name="aFunction">The method to call for the message</param>
        /// <param name="aAttrs">The message attributes</param>
        public ButtplugDeviceMessageHandler(Func<ButtplugDeviceMessage, CancellationToken, Task<ButtplugMessage>> aFunction,
            MessageAttributes aAttrs = null)
        {
            Function = aFunction;
            Attrs = aAttrs ?? new MessageAttributes();
        }
    }
}