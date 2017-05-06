using LanguageExt;

namespace Buttplug.Core
{
    public interface IButtplugMessage
    {
        Option<string> Check();
    }

    public interface IButtplugDeviceMessage : IButtplugMessage
    {
        uint DeviceIndex { get; }
    }
}
