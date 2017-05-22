using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Messages;
using SharpDX.XInput;

namespace ButtplugXInputGamepadManager.Devices
{
    internal class XInputGamepadDevice : ButtplugDevice
    {
        private readonly Controller _device;

        public XInputGamepadDevice(IButtplugLogManager aLogManager, Controller d) :
            base(aLogManager, "XBox Compatible Gamepad (XInput)", d.UserIndex.ToString())
        {
            _device = d;
            MsgFuncs.Add(typeof(SingleMotorVibrateCmd), HandleSingleMotorVibrateCmd);
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
    }
}