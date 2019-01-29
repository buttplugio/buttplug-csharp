namespace Buttplug.Devices.Configuration
{
    public class HIDProtocolConfiguration : IProtocolConfiguration
    {
        public readonly ushort VendorId;
        public readonly ushort ProductId;

        public HIDProtocolConfiguration(ushort aVendorId, ushort aProductId)
        {
            VendorId = aVendorId;
            ProductId = aProductId;
        }

        internal HIDProtocolConfiguration(HIDIdentifier aId, HIDConfiguration aConfig)
        {
            VendorId = aId.VendorId;
            ProductId = aId.ProductId;
        }

        public bool Matches(IProtocolConfiguration aConfig)
        {
            return aConfig is HIDProtocolConfiguration hidConfig && hidConfig.ProductId == ProductId && hidConfig.VendorId == VendorId;
        }
    }
}