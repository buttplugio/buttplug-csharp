using System;
using Newtonsoft.Json;

namespace Buttplug.Core
{
    public class ButtplugDeviceMessage : ButtplugMessage
    {
        [JsonProperty(Required = Required.Always)]
        public uint DeviceIndex { get; set; }

        public ButtplugDeviceMessage(uint aId, uint aDeviceIndex, uint aSchemaVersion = 0, Type aPreviousType = null)
            : base(aId, aSchemaVersion, aPreviousType)
        {
            DeviceIndex = aDeviceIndex;
        }
    }
}