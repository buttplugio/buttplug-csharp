using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;
using SharpDX.XInput;

namespace Buttplug.Server.Managers.XInputGamepadManager
{
    internal class XInputGamepadDevice : ButtplugDevice
    {
        private readonly Controller _device;

        public XInputGamepadDevice(IButtplugLogManager aLogManager, Controller aDevice)
            : base(aLogManager, "XBox Compatible Gamepad (XInput)", aDevice.UserIndex.ToString())
        {
            _device = aDevice;
            MsgFuncs.Add(typeof(SingleMotorVibrateCmd), HandleSingleMotorVibrateCmd);
            MsgFuncs.Add(typeof(StopDeviceCmd), HandleStopDeviceCmd);
        }

        private Task<ButtplugMessage> HandleStopDeviceCmd(ButtplugDeviceMessage aMsg)
        {
            return HandleSingleMotorVibrateCmd(new SingleMotorVibrateCmd(aMsg.DeviceIndex, 0, aMsg.Id));
        }

        private Task<ButtplugMessage> HandleSingleMotorVibrateCmd(ButtplugDeviceMessage aMsg)
        {
            var cmdMsg = aMsg as SingleMotorVibrateCmd;
            if (cmdMsg is null)
            {
                return Task.FromResult<ButtplugMessage>(BpLogger.LogErrorMsg(aMsg.Id, Error.ErrorClass.ERROR_DEVICE, "Wrong Handler"));
            }

            var v = new Vibration()
            {
                LeftMotorSpeed = (ushort)(cmdMsg.Speed * 65536),
                RightMotorSpeed = (ushort)(cmdMsg.Speed * 65536),
            };
            _device.SetVibration(v);
            return Task.FromResult<ButtplugMessage>(new Ok(aMsg.Id));
        }
    }
}
