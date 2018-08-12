using System;
using Newtonsoft.Json;

namespace Buttplug.Core
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
        /// <param name="aSchemaVersion">Version of the schema where the message was introduced. Required for cross-version schema support.</param>
        /// <param name="aPreviousType">Previous version type of the message, or null. This is used to downgrade messages when communicating with older clients.</param>
        public ButtplugDeviceMessage(uint aId, uint aDeviceIndex, uint aSchemaVersion = 0, Type aPreviousType = null)
            : base(aId, aSchemaVersion, aPreviousType)
        {
            DeviceIndex = aDeviceIndex;
        }
    }
}