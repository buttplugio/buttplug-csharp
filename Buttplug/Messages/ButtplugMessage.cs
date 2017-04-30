using System;
using Buttplug;

namespace Buttplug.Messages
{
    public class FleshlightLaunchRawMessage : IButtplugDeviceMessage
    {
        public UInt32 DeviceIndex { get; }
        public readonly UInt16 Speed;
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
        public UInt32 DeviceIndex { get; }
    }

    public class SingleMotorVibrateMessage : IButtplugDeviceMessage
    {
        public UInt32 DeviceIndex { get; }
        public Double Speed { get; }

        public SingleMotorVibrateMessage(UInt32 d, Double speed)
        {
            DeviceIndex = d;
            Speed = speed;
        }
    }

    public class VectorSpeedMessage : IButtplugDeviceMessage
    {
        public UInt32 DeviceIndex { get; }
    }
}
