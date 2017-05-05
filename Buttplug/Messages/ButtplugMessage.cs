using System;
using Buttplug;
using Buttplug.Core;
using Newtonsoft.Json;

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
    }

    public class FleshlightLaunchRawMessage : IButtplugDeviceMessage
    {
        [JsonProperty(Required = Required.Always)]
        public uint DeviceIndex { get; set; }
        [JsonProperty(Required = Required.Always)]
        public readonly ushort Speed;
        [JsonProperty(Required = Required.Always)]
        public readonly ushort Position;

        FleshlightLaunchRawMessage(uint aDeviceIndex, ushort aSpeed, ushort aPosition)
        {
            DeviceIndex = aDeviceIndex;
            Speed = aSpeed;
            Position = aPosition;
        }
    }

    public class LovenseRawMessage : IButtplugDeviceMessage
    {
        [JsonProperty(Required = Required.Always)]
        public uint DeviceIndex { get; }
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
        }
    }

    public class VectorSpeedMessage : IButtplugDeviceMessage
    {
        [JsonProperty(Required = Required.Always)]
        public uint DeviceIndex { get; }
    }

    public class PingMessage : IButtplugMessage
    {
        [JsonProperty(Required = Required.Always)]
        public bool PingBool { get; }

        public PingMessage(bool b)
        {
            PingBool = b;
        }
    }

    public class TestMessage : IButtplugMessage
    {
        [JsonProperty(Required = Required.Always)]
        public string TestString { get; }

        public TestMessage(string aString)
        {
            TestString = aString;
        }
    }
}
