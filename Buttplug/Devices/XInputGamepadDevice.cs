using System;
using System.Threading.Tasks;
using SharpDX.XInput;
using Buttplug.Messages;

namespace Buttplug.Devices
{
    class XInputGamepadDevice : ButtplugDevice
    {
        Controller Device;

        public XInputGamepadDevice(Controller d) :
            base("XBox Compatible Gamepad (XInput)")
        {
            Device = d;
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
