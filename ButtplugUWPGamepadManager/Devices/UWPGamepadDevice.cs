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

        public UwpGamepadDevice(IButtplugLogManager aLogManager, Gamepad aDevice) :
            base(aLogManager, "XBox Compatible Gamepad (UWP)", aDevice.User.ToString())
        {
            _device = aDevice;
            MsgFuncs.Add(typeof(SingleMotorVibrateCmd), HandleSingleMotorVibrateCmd);
        }

        private Task<ButtplugMessage> HandleSingleMotorVibrateCmd(ButtplugDeviceMessage aMsg)
        {
            var cmdMsg = aMsg as SingleMotorVibrateCmd;
            if (cmdMsg is null)
            {
                return Task.FromResult<ButtplugMessage>(BpLogger.LogErrorMsg(aMsg.Id, ErrorClass.ERROR_DEVICE, "Wrong Handler"));
            }
            var v = new GamepadVibration()
            {
                LeftMotor = cmdMsg.Speed,
                RightMotor = cmdMsg.Speed
            };
            _device.Vibration = v;
            return Task.FromResult<ButtplugMessage>(new Ok(aMsg.Id));
        }
    }
}