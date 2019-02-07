using System;

namespace Buttplug.Devices.Configuration
{
    public class USBProtocolConfiguration : IProtocolConfiguration
    {
        public readonly ushort VendorId;
        public readonly ushort ProductId;

        public USBProtocolConfiguration(ushort aVendorId, ushort aProductId)
        {
            VendorId = aVendorId;
            ProductId = aProductId;
        }

        internal USBProtocolConfiguration(USBInfo aInfo)
        {
            VendorId = aInfo.VendorId;
            ProductId = aInfo.ProductId;
        }

        public bool Matches(IProtocolConfiguration aConfig)
        {
            return aConfig is USBProtocolConfiguration usbConfig && usbConfig.ProductId == ProductId && usbConfig.VendorId == VendorId;
        }

        public void Merge(IProtocolConfiguration aConfig)
        {
            throw new NotImplementedException("No valid implementation of configuration merging for USB");
        }
    }
}