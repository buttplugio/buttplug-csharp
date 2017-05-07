using System;
using System.Threading.Tasks;
using Buttplug.Core;
using SharpDX.XInput;
using Buttplug.Messages;

namespace Buttplug.Devices
{
    class XInputGamepadDevice : ButtplugDevice, IEquatable<XInputGamepadDevice>
    {
        private readonly Controller _device;

        public XInputGamepadDevice(Controller d) :
            base("XBox Compatible Gamepad (XInput)")
        {
            _device = d;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as XInputGamepadDevice);
        }

        public bool Equals(XInputGamepadDevice other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            // This could get weird based on connects/disconnects?
            return _device.UserIndex == other._device.UserIndex;
        }

#pragma warning disable 1998
        public override async Task<bool> ParseMessage(IButtplugDeviceMessage aMsg)
#pragma warning restore 1998
        {
            switch (aMsg)
            {
                case Messages.SingleMotorVibrateCmd m:
                    var v = new Vibration()
                    {
                        LeftMotorSpeed = (ushort)(m.Speed * 65536),
                        RightMotorSpeed = (ushort)(m.Speed * 65536)
                    };
                    _device.SetVibration(v);
                    return true;
            }
            return false;
        }
    }
}
