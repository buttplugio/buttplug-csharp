using LanguageExt;

namespace Buttplug.Core
{
    public interface IButtplugMessage
    {
    }

    public interface IButtplugMessageOutgoingOnly : IButtplugMessage
    {
    }

    public interface IButtplugDeviceMessage : IButtplugMessage
    {
        uint DeviceIndex { get; }
    }
}
