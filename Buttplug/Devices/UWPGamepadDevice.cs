using Buttplug.Core;
using Buttplug.Messages;
using LanguageExt;
using NLog;
using System;
using System.Threading.Tasks;
using Windows.Gaming.Input;

namespace Buttplug.Devices
{
    internal class UwpGamepadDevice : ButtplugDevice, IEquatable<UwpGamepadDevice>
    {
        private readonly Gamepad _device;

        public UwpGamepadDevice(Gamepad d) :
            base("XBox Compatible Gamepad (UWP)")
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
                return ButtplugUtils.LogAndError(aMsg.Id, BpLogger, LogLevel.Error, "Wrong Handler");
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