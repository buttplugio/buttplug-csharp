using Buttplug;
using Buttplug.Core;
using Newtonsoft.Json;
using LanguageExt;

namespace Buttplug.Messages
{

    public class DeviceAddedMessage : IButtplugDeviceMessage
    {
        [JsonProperty(Required = Required.Always)]
        public string DeviceName { get; }
        [JsonProperty(Required = Required.Always)]
        public uint DeviceIndex { get; }

        public DeviceAddedMessage(uint aIndex, string aName)
        {
            DeviceName = aName;
            DeviceIndex = aIndex;
        }

        public Option<string> Check()
        {
            return new OptionNone();
        }
    }

    public class FleshlightLaunchRawMessage : IButtplugDeviceMessage
    {
        [JsonProperty(Required = Required.Always)]
        public uint DeviceIndex { get; set; }
        [JsonProperty(Required = Required.Always)]
        public readonly ushort Speed;
        [JsonProperty(Required = Required.Always)]
        public readonly ushort Position;

        public FleshlightLaunchRawMessage(uint aDeviceIndex, ushort aSpeed, ushort aPosition)
        {
            DeviceIndex = aDeviceIndex;
            Speed = aSpeed;
            Position = aPosition;
            Check();
        }

        public Option<string> Check()
        {
            if (Speed > 99)
            {
                return Option<string>.Some("FleshlightLaunchRawMessage cannot have a speed higher than 99!");
            }
            if (Position > 99)
            {
                return Option<string>.Some("FleshlightLaunchRawMessage cannot have a position higher than 99!");
            }
            return new OptionNone();
        }
    }

    public class LovenseRawMessage : IButtplugDeviceMessage
    {
        [JsonProperty(Required = Required.Always)]
        public uint DeviceIndex { get; }

        public Option<string> Check()
        {
            return new OptionNone();
        }
    }

    public class SingleMotorVibrateMessage : IButtplugDeviceMessage
    {
        [JsonProperty(Required = Required.Always)]
        public uint DeviceIndex { get; set; }
        [JsonProperty(Required = Required.Always)]
        public double Speed { get; }

        public SingleMotorVibrateMessage(uint d, double speed)
        {
            DeviceIndex = d;
            Speed = speed;
            Check();
        }

        public Option<string> Check()
        {
            if (Speed < 0)
            {
                return Option<string>.Some("SingleMotorVibrateMessage Speed cannot be less than 0!");
            }
            if (Speed > 1)
            {
                return Option<string>.Some("SingleMotorVibrateMessage Speed cannot be greater than 1!");
            }
            return new OptionNone();
        }
    }

    public class PingMessage : IButtplugMessage
    {
        public Option<string> Check() { return new OptionNone(); }
    }

    public class TestMessage : IButtplugMessage
    {
        [JsonProperty(Required = Required.Always)]
        public string TestString { get; set;  }

        public TestMessage(string aString)
        {
            TestString = aString;
        }

        public Option<string> Check()
        {
            if (TestString == "Error")
            {
                return Option<string>.Some("Got an error message!");
            }
            return new OptionNone();
        }
    }

    public class ErrorMessage : IButtplugMessage
    {
        [JsonProperty(Required = Required.Always)]
        public string ErrorString { get; }

        public ErrorMessage(string aErrorString)
        {
            ErrorString = aErrorString;
        }

        public Option<string> Check() { return new OptionNone(); }
    }
}
