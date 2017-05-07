using System;
using System.Threading.Tasks;
using Windows.Gaming.Input;
using Buttplug.Core;
using Buttplug.Messages;
using LanguageExt;
using NLog;

namespace Buttplug.Devices
{
    internal class UwpGamepadDevice : ButtplugDevice, IEquatable<UwpGamepadDevice>
    {
        private readonly Gamepad _device;

        public UwpGamepadDevice(Gamepad d) :
            base("XBox Compatible Gamepad (UWP)")
        {
            _device = d;
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
        public override async Task<Either<Error, IButtplugMessage>> ParseMessage(IButtplugDeviceMessage aMsg)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            switch (aMsg)
            {
                case Messages.SingleMotorVibrateCmd m:
                    var v = new GamepadVibration()
                    {
                        LeftMotor = m.Speed,
                        RightMotor = m.Speed
                    };
                    _device.Vibration = v;
                    return new Ok();
            }
            return ButtplugUtils.LogAndError(BpLogger, LogLevel.Error, $"{Name} cannot handle message of type {aMsg.GetType().Name}");
        }
    }
}