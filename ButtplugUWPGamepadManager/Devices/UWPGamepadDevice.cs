using System.Threading.Tasks;
using Windows.Gaming.Input;
using Buttplug.Core;
using Buttplug.Messages;
using static Buttplug.Messages.Error;

namespace ButtplugUWPGamepadManager.Devices
{
    internal class UwpGamepadDevice : ButtplugDevice
    {
        private readonly Gamepad _device;

        public UwpGamepadDevice(IButtplugLogManager aLogManager, Gamepad d) :
            base(aLogManager, "XBox Compatible Gamepad (UWP)", d.User.ToString())
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
                return BpLogger.LogErrorMsg(aMsg.Id, ErrorClass.ERROR_DEVICE, "Wrong Handler");
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