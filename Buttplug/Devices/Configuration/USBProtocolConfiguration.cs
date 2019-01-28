namespace Buttplug.Devices.Configuration
{
    public class USBProtocolConfiguration : IProtocolConfiguration
    {
        public readonly string ProtocolName;
        public readonly ushort VendorId;
        public readonly ushort ProductId;

        public USBProtocolConfiguration(string aProtocolName, ushort aVendorId, ushort aProductId)
        {
            ProtocolName = aProtocolName;
            VendorId = aVendorId;
            ProductId = aProductId;
        }

        public bool Matches(IProtocolConfiguration aConfig)
        {
            return aConfig is USBProtocolConfiguration usbConfig && usbConfig.ProductId == ProductId && usbConfig.VendorId == VendorId;
        }
    }
}