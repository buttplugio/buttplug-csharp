using System;
using Buttplug.Core.Messages;
using Newtonsoft.Json;

namespace Buttplug.Core.Devices
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
        /// <param name="aId">Message ID</param>
        /// <param name="aDeviceIndex">Device index</param>
        public ButtplugDeviceMessage(uint aId = ButtplugConsts.DefaultMsgId, uint aDeviceIndex = UInt32.MaxValue)
            : base(aId)
        {
            DeviceIndex = aDeviceIndex;
        }
    }
}