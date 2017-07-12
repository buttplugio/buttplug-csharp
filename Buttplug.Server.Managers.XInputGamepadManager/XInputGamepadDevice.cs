using System;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;
using SharpDX.XInput;

namespace Buttplug.Server.Managers.XInputGamepadManager
{
    internal class XInputGamepadDevice : ButtplugDevice
    {
        private Controller _device;

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

            var v = new Vibration
            {
                LeftMotorSpeed = (ushort)(cmdMsg.Speed * ushort.MaxValue),
                RightMotorSpeed = (ushort)(cmdMsg.Speed * ushort.MaxValue),
            };

            try
            {
                _device?.SetVibration(v);
            }
            catch (Exception e)
            {
                if (_device?.IsConnected != true)
                {
                    InvokeDeviceRemoved();

                    // Don't throw a spanner in the works
                    return Task.FromResult<ButtplugMessage>(new Ok(aMsg.Id));
                }

                return Task.FromResult<ButtplugMessage>(BpLogger.LogErrorMsg(aMsg.Id, Error.ErrorClass.ERROR_DEVICE, e.Message));
            }

            return Task.FromResult<ButtplugMessage>(new Ok(aMsg.Id));
        }

        public override void Disconnect()
        {
            var v = new Vibration
            {
                LeftMotorSpeed = 0,
                RightMotorSpeed = 0,
            };
            try
            {
                _device?.SetVibration(v);
                _device = null;
            }
            finally
            {
                _device = null;
            }
        }
    }
}
