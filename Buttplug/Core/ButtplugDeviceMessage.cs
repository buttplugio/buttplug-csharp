using Newtonsoft.Json;

namespace Buttplug.Core
{
    public class ButtplugDeviceMessage : ButtplugMessage
    {
        [JsonProperty(Required = Required.Always)]
        public uint DeviceIndex { get; set; }

        public ButtplugDeviceMessage(uint aId, uint aDeviceIndex) : base(aId)
        {
            DeviceIndex = aDeviceIndex;
        }
    }
}