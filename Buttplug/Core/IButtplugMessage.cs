using LanguageExt;
using Newtonsoft.Json;

namespace Buttplug.Core
{
    public class ButtplugMessage
    {
        [JsonProperty(Required = Required.Always)]
        public uint Id { get; set; }

        public ButtplugMessage(uint aId)
        {
            Id = aId;
        }
    }

    public interface IButtplugMessageOutgoingOnly
    {
    }

    public class ButtplugDeviceMessage : ButtplugMessage
    {
        [JsonProperty(Required = Required.Always)]
        public uint DeviceIndex { get; }

        public ButtplugDeviceMessage(uint aId, uint aDeviceIndex) : base(aId)
        {
            DeviceIndex = aDeviceIndex;
        }
    }
}
