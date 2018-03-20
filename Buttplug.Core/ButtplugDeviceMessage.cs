using System;
using Newtonsoft.Json;

namespace Buttplug.Core
{
    /// <summary>
    /// The device command subset of Buttplug messages
    /// </summary>
    public class ButtplugDeviceMessage : ButtplugMessage
    {
        /// <summary>
        /// Gets or sets the index of the device this message is intended to act upon.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public uint DeviceIndex { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ButtplugDeviceMessage"/> class.
        /// </summary>
        /// <param name="aId">The message ID</param>
        /// <param name="aDeviceIndex">The device index</param>
        /// <param name="aSchemaVersion">The version of the schema that the message was introduced. Required for cross version schema support.</param>
        /// <param name="aPreviousType">The Type for the previous version of the message, or null. This is used to downgrade messages when cominucating with older clients.</param>
        public ButtplugDeviceMessage(uint aId, uint aDeviceIndex, uint aSchemaVersion = 0, Type aPreviousType = null)
            : base(aId, aSchemaVersion, aPreviousType)
        {
            DeviceIndex = aDeviceIndex;
        }
    }
}