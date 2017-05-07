using LanguageExt;

namespace Buttplug.Core
{
    public interface IButtplugMessage
    {
        Option<string> Check();
    }

    public class ButtplugMessageNoBody : IButtplugMessage
    {
        public Option<string> Check()
        {
            return new OptionNone();
        }
    }

    public interface IButtplugMessageOutgoingOnly : IButtplugMessage
    {
    }

    public interface IButtplugDeviceMessage : IButtplugMessage
    {
        uint DeviceIndex { get; }
    }
}
