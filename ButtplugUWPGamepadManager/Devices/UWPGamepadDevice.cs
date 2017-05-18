using System;
using System.Threading.Tasks;
using Windows.Gaming.Input;
using Buttplug.Core;
using Buttplug.Messages;

namespace ButtplugUWPGamepadManager.Devices
{
    internal class UwpGamepadDevice : ButtplugDevice, IEquatable<UwpGamepadDevice>
    {
        private readonly Gamepad _device;

        public UwpGamepadDevice(IButtplugLogManager aLogManager, Gamepad d) :
            base(aLogManager, "XBox Compatible Gamepad (UWP)")
        {
            _device = d;
            MsgFuncs.Add(typeof(SingleMotorVibrateCmd), HandleSingleMotorVibrateCmd);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as UwpGamepadDevice);
        }

        public bool Equals(UwpGamepadDevice other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }
            // UWP Should always hand us matching devices.
            return _device == other._device;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        public async Task<ButtplugMessage> HandleSingleMotorVibrateCmd(ButtplugDeviceMessage aMsg)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            var cmdMsg = aMsg as SingleMotorVibrateCmd;
            if (cmdMsg is null)
            {
                return BpLogger.LogErrorMsg(aMsg.Id, "Wrong Handler");
            }
            var v = new GamepadVibration()
            {
                LeftMotor = cmdMsg.Speed,
                RightMotor = cmdMsg.Speed
            };
            _device.Vibration = v;
            return new Ok(aMsg.Id);
        }
    }
}