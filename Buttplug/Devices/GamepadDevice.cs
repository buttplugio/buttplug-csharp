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
    class GamepadDevice : IButtplugDevice
    {
        public Gamepad Device { get; }
        public String Name { get; }

        public GamepadDevice(Gamepad d)
        {
            Name = "XBox Compatible Gamepad";
            Device = d;
        }

        public bool ParseMessage(IButtplugDeviceMessage aMsg)
        {
            switch (aMsg)
            {
                case SingleMotorVibrateMessage m:
                    return true;
            }
            return false;
        }
    }
}
