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
    class UWPGamepadDevice : ButtplugDevice
    {
        Gamepad Device;

        public UWPGamepadDevice(Gamepad d) :
            base("XBox Compatible Gamepad (UWP)")
        {
            Device = d;
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