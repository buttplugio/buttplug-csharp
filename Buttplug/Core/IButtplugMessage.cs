using System;

namespace Buttplug
{
    public interface IButtplugMessage
    {
    }

    public interface IButtplugDeviceMessage : IButtplugMessage
    {
        UInt32 DeviceIndex { get; }
    }
}
