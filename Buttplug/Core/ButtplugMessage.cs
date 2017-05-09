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
}
