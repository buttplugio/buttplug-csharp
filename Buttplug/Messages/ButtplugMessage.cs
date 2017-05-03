using System;
using Buttplug;
using Newtonsoft.Json;

namespace Buttplug.Messages
{
    public class DeviceAddedMessage : IButtplugDeviceMessage
    {
        [JsonProperty(Required = Required.Always)]
        public String DeviceName { get; }
        [JsonProperty(Required = Required.Always)]
        public UInt32 DeviceIndex { get; }

        public DeviceAddedMessage(UInt32 aIndex, String aName)
        {
            DeviceName = aName;
            DeviceIndex = aIndex;
        }
    }

    public class FleshlightLaunchRawMessage : IButtplugDeviceMessage
    {
        [JsonProperty(Required = Required.Always)]
        public UInt32 DeviceIndex { get; set; }
        [JsonProperty(Required = Required.Always)]
        public readonly UInt16 Speed;
        [JsonProperty(Required = Required.Always)]
        public readonly UInt16 Position;

        FleshlightLaunchRawMessage(UInt32 aDeviceIndex, UInt16 aSpeed, UInt16 aPosition)
        {
            DeviceIndex = aDeviceIndex;
            Speed = aSpeed;
            Position = aPosition;
        }
    }

    public class LovenseRawMessage : IButtplugDeviceMessage
    {
        [JsonProperty(Required = Required.Always)]
        public UInt32 DeviceIndex { get; }
    }

    public class SingleMotorVibrateMessage : IButtplugDeviceMessage
    {
        [JsonProperty(Required = Required.Always)]
        public UInt32 DeviceIndex { get; set; }
        [JsonProperty(Required = Required.Always)]
        public Double Speed { get; }

        public SingleMotorVibrateMessage(UInt32 d, Double speed)
        {
            DeviceIndex = d;
            Speed = speed;
        }
    }

    public class VectorSpeedMessage : IButtplugDeviceMessage
    {
        [JsonProperty(Required = Required.Always)]
        public UInt32 DeviceIndex { get; }
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
}
