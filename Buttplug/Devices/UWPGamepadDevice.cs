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
    class UWPGamepadDevice : IButtplugDevice
    {
        Gamepad Device;
        public String Name { get; }

        public UWPGamepadDevice(Gamepad d)
        {
            Name = "XBox Compatible Gamepad (UWP)";
            Device = d;
        }

        public bool ParseMessage(IButtplugDeviceMessage aMsg)
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