using System;
using Newtonsoft.Json;

namespace Buttplug.Devices.Configuration
{
    public class HIDProtocolConfiguration : ProtocolConfiguration
    {
        [JsonProperty("vendor-id")]
        public readonly ushort VendorId;
        [JsonProperty("product-id")]
        public readonly ushort ProductId;

        public HIDProtocolConfiguration()
        { }

        public HIDProtocolConfiguration(ushort aVendorId, ushort aProductId)
        {
            VendorId = aVendorId;
            ProductId = aProductId;
        }

        public override bool Matches(IProtocolConfiguration aConfig)
        {
            return aConfig is HIDProtocolConfiguration hidConfig && hidConfig.ProductId == ProductId && hidConfig.VendorId == VendorId;
        }

        public override void Merge(IProtocolConfiguration aConfig)
        {
            throw new NotImplementedException("No valid implementation of configuration merging for HID");
        }
    }
}