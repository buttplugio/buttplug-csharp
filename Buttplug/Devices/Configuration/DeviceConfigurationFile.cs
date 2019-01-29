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

        public Dictionary<string, ProtocolInfo> Protocols { get; set; }
    }

    internal class ProtocolInfo
    {
        [YamlMember(Alias = "id", ApplyNamingConventions = false)]
        public ProtocolIdentifier Identifier { get; set; }

        [YamlMember(Alias = "config", ApplyNamingConventions = false)]
        public ProtocolConfiguration Configuration { get; set; }
    }

    internal class BluetoothLEIdentifier
    {
        public List<string> Names { get; set; }

        public List<Guid> Services { get; set; }
    }

    internal class USBIdentifier
    {
        public ushort VendorId { get; set; }

        public ushort ProductId { get; set; }
    }

    internal class HIDIdentifier
    {
        public ushort VendorId { get; set; }

        public ushort ProductId { get; set; }
    }

    internal class SerialIdentifier
    { }

    internal class ProtocolIdentifier
    {
        public BluetoothLEIdentifier Btle { get; set; }

        public USBIdentifier Usb { get; set; }

        public HIDIdentifier Hid { get; set; }

        public SerialIdentifier Serial { get; set; }
    }

    internal class USBConfiguration
    { }

    internal class HIDConfiguration
    { }

    internal class SerialConfiguration
    {
        public uint BaudRate { get; set; }

        public byte DataBits { get; set; }

        public char Parity { get; set; }

        public byte StopBits { get; set; }
    }

    internal class ProtocolConfiguration
    {
        public Dictionary<Guid, Dictionary<string, Guid>> Btle { get; set; }

        public USBConfiguration Usb { get; set; }

        public HIDConfiguration Hid { get; set; }

        public SerialConfiguration Serial { get; set; }
    }
}
