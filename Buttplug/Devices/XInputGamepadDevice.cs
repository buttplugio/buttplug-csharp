using System;
using System.Threading.Tasks;
using SharpDX.XInput;
using Buttplug.Messages;

namespace Buttplug.Devices
{
    class XInputGamepadDevice : ButtplugDevice, IEquatable<XInputGamepadDevice>
    {
        Controller Device { get; }

        public XInputGamepadDevice(Controller d) :
            base("XBox Compatible Gamepad (XInput)")
        {
            Device = d;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as XInputGamepadDevice);
        }

        public bool Equals(XInputGamepadDevice other)
        {
            if (Object.ReferenceEquals(other, null))
            {
                return false;
            }

            if (Object.ReferenceEquals(this, other))
            {
                return true;
            }

            // This could get weird based on connects/disconnects?
            return Device.UserIndex == other.Device.UserIndex;
        }

        public async override Task<bool> ParseMessage(IButtplugDeviceMessage aMsg)
        {
            switch (aMsg)
            {
                case SingleMotorVibrateMessage m:
                    var v = new Vibration();
                    v.LeftMotorSpeed = (ushort)(m.Speed * 65536);
                    v.RightMotorSpeed = (ushort)(m.Speed * 65536);
                    Device.SetVibration(v);
                    return true;
            }
            return false;
        }
    }
}
