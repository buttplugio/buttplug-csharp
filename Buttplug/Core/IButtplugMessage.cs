using System;

namespace Buttplug.Core
{
    public interface IButtplugMessage
    {
    }

    public interface IButtplugDeviceMessage : IButtplugMessage
    {
        uint DeviceIndex { get; }
    }
}
