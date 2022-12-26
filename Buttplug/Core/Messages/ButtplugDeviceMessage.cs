using System;
using Newtonsoft.Json;

namespace Buttplug.Core.Messages
{
    /// <summary>
    /// Subclass of Buttplug Messages, that command a device to take an action.
    /// </summary>
    public class ButtplugDeviceMessage : ButtplugMessage
    {
        /// <summary>
        /// Device index the message is intended for.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public uint DeviceIndex { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ButtplugDeviceMessage"/> class.
        /// </summary>
        /// <param name="aId">Message ID.</param>
        /// <param name="aDeviceIndex">Device index.</param>
        public ButtplugDeviceMessage(uint aId = ButtplugConsts.DefaultMsgId, uint aDeviceIndex = uint.MaxValue)
            : base(aId)
        {
            this.DeviceIndex = aDeviceIndex;
        }
    }
}