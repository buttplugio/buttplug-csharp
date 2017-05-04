using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Gaming.Input;
using Buttplug.Messages;
using Buttplug;

namespace Buttplug.Devices
{
    class UWPGamepadDevice : ButtplugDevice, IEquatable<UWPGamepadDevice>
    {
        Gamepad Device { get; }

        public UWPGamepadDevice(Gamepad d) :
            base("XBox Compatible Gamepad (UWP)")
        {
            Device = d;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as UWPGamepadDevice);
        }

        public bool Equals(UWPGamepadDevice other)
        {
            if (Object.ReferenceEquals(other, null))
            {
                return false;
            }

            if (Object.ReferenceEquals(this, other))
            {
                return true;
            }
            // UWP Should always hand us matching devices.
            return Device == other.Device;
        }

        public override async Task<bool> ParseMessage(IButtplugDeviceMessage aMsg)
        {
            switch (aMsg)
            {
                case SingleMotorVibrateMessage m:
                    GamepadVibration v = new GamepadVibration()
                    {
                        LeftMotor = m.Speed,
                        RightMotor = m.Speed
                    };
                    Device.Vibration = v;
                    return true;
            }
            return false;
        }
    }
}