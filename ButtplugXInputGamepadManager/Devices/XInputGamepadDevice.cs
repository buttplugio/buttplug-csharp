using System;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Messages;
using SharpDX.XInput;

namespace ButtplugXInputGamepadManager.Devices
{
    internal class XInputGamepadDevice : ButtplugDevice, IEquatable<XInputGamepadDevice>
    {
        private readonly Controller _device;

        public XInputGamepadDevice(IButtplugLogManager aLogManager, Controller d) :
            base(aLogManager, "XBox Compatible Gamepad (XInput)")
        {
            _device = d;
            MsgFuncs.Add(typeof(SingleMotorVibrateCmd), HandleSingleMotorVibrateCmd);
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

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        public async Task<ButtplugMessage> HandleSingleMotorVibrateCmd(ButtplugDeviceMessage aMsg)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            var cmdMsg = aMsg as SingleMotorVibrateCmd;
            if (cmdMsg is null)
            {
                return BpLogger.LogErrorMsg(aMsg.Id, "Wrong Handler");
            }
            var v = new Vibration()
            {
                LeftMotorSpeed = (ushort) (cmdMsg.Speed * 65536),
                RightMotorSpeed = (ushort) (cmdMsg.Speed * 65536)
            };
            _device.SetVibration(v);
            return new Ok(aMsg.Id);
        }

        public override async Task<ButtplugMessage> Initialize()
        {
            return new Ok(ButtplugConsts.SYSTEM_MSG_ID);
        }
    }
}