using System;
using Newtonsoft.Json;

namespace Buttplug.Devices.Configuration
{
    public class USBProtocolConfiguration : ProtocolConfiguration
    {
        [JsonProperty("vendor-id")]
        public readonly ushort VendorId;
        [JsonProperty("product-id")]
        public readonly ushort ProductId;

        public USBProtocolConfiguration()
        { }

        public USBProtocolConfiguration(ushort aVendorId, ushort aProductId)
        {
            VendorId = aVendorId;
            ProductId = aProductId;
        }

        public override bool Matches(IProtocolConfiguration aConfig)
        {
            return aConfig is USBProtocolConfiguration usbConfig && usbConfig.ProductId == ProductId && usbConfig.VendorId == VendorId;
        }

        public override void Merge(IProtocolConfiguration aConfig)
        {
            throw new NotImplementedException("No valid implementation of configuration merging for USB");
        }
    }
}