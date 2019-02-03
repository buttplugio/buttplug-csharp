using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using YamlDotNet.Serialization;

namespace Buttplug.Devices.Configuration
{
    internal class DeviceConfigurationFile
    {
        [YamlMember(Alias = "connection-info-types", ApplyNamingConventions = false)]
        public List<string> ConnectionInfoTypes { get; set; }

        public Dictionary<string, DeviceTypeInfo> Protocols { get; set; }
    }

    internal class DeviceTypeInfo
    {
        public BluetoothLEInfo Btle { get; set; }

        public USBInfo Usb { get; set; }

        public HIDInfo Hid { get; set; }

        public SerialInfo Serial { get; set; }
    }

    internal class BluetoothLEInfo
    {
        public List<string> Names { get; set; }

        public Dictionary<Guid, Dictionary<string, Guid>> Services { get; set; }
    }

    internal class USBInfo
    {
        public ushort VendorId { get; set; }

        public ushort ProductId { get; set; }
    }

    internal class HIDInfo
    {
        public ushort VendorId { get; set; }

        public ushort ProductId { get; set; }
    }

    internal class SerialInfo
    {
        public uint BaudRate { get; set; }

        public byte DataBits { get; set; }

        public char Parity { get; set; }

        public byte StopBits { get; set; }
    }
}
