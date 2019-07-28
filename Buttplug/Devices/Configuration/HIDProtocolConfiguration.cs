using System;
using Newtonsoft.Json;

namespace Buttplug.Devices.Configuration
{
    public class HIDProtocolConfiguration : IProtocolConfiguration
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

        public bool Matches(IProtocolConfiguration aConfig)
        {
            return aConfig is HIDProtocolConfiguration hidConfig && hidConfig.ProductId == ProductId && hidConfig.VendorId == VendorId;
        }

        public void Merge(IProtocolConfiguration aConfig)
        {
            throw new NotImplementedException("No valid implementation of configuration merging for HID");
        }
    }
}