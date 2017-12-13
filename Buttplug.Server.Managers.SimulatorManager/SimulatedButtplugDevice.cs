using System;
using System.Linq;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace Buttplug.Server.Managers.SimulatorManager
{
    internal class SimulatedButtplugDevice : ButtplugDevice
    {
        private SimulatorManager _manager;
        private uint _vibratorCount = 0;

        public SimulatedButtplugDevice(
            SimulatorManager aManager,
            [NotNull] IButtplugLogManager aLogManager,
            [NotNull] DeviceSimulator.PipeMessages.DeviceAdded da)
            : base(aLogManager, da.Name, da.Id)
        {
            _manager = aManager;
            if (da.HasLinear)
            {
                MsgFuncs.Add(typeof(FleshlightLaunchFW12Cmd), new ButtplugDeviceWrapper(HandleFleshlightLaunchFW12Cmd));
                MsgFuncs.Add(typeof(LinearCmd), new ButtplugDeviceWrapper(HandleLinearCmd, new MessageAttributes() { FeatureCount = 1 }));
            }

            if (da.VibratorCount > 0)
            {
                _vibratorCount = da.VibratorCount;
                MsgFuncs.Add(typeof(SingleMotorVibrateCmd), new ButtplugDeviceWrapper(HandleSingleMotorVibrateCmd));
                MsgFuncs.Add(typeof(VibrateCmd), new ButtplugDeviceWrapper(HandleVibrateCmd, new MessageAttributes() { FeatureCount = da.VibratorCount }));
            }

            if (da.HasRotator)
            {
                MsgFuncs.Add(typeof(VorzeA10CycloneCmd), new ButtplugDeviceWrapper(HandleVorzeA10CycloneCmd));
            }

            MsgFuncs.Add(typeof(StopDeviceCmd), new ButtplugDeviceWrapper(HandleStopDeviceCmd));
        }

        public override void Disconnect()
        {
        }

        private async Task<ButtplugMessage> HandleStopDeviceCmd(ButtplugDeviceMessage aMsg)
        {
            _manager.StopDevice(this);
            return new Ok(aMsg.Id);
        }

        private async Task<ButtplugMessage> HandleSingleMotorVibrateCmd(ButtplugDeviceMessage aMsg)
        {
            for (uint i = 0; i < _vibratorCount; i++)
            {
                _manager.Vibrate(this, (aMsg as SingleMotorVibrateCmd).Speed, i);
            }

            return new Ok(aMsg.Id);
        }

        private async Task<ButtplugMessage> HandleVibrateCmd(ButtplugDeviceMessage aMsg)
        {
            var vis = from x in (aMsg as VibrateCmd).Speeds where x.Index >= 0 && x.Index < _vibratorCount select x;
            if (!vis.Any())
            {
                return new Error("Invalid vibrator index!", Error.ErrorClass.ERROR_DEVICE, aMsg.Id);
            }

            foreach (var vi in vis)
            {
                _manager.Vibrate(this, vi.Speed, vi.Index);
            }

            return new Ok(aMsg.Id);
        }

        private async Task<ButtplugMessage> HandleVorzeA10CycloneCmd(ButtplugDeviceMessage aMsg)
        {
            _manager.Rotate(this, (aMsg as VorzeA10CycloneCmd).Speed, (aMsg as VorzeA10CycloneCmd).Clockwise);
            return new Ok(aMsg.Id);
        }

        private async Task<ButtplugMessage> HandleFleshlightLaunchFW12Cmd(ButtplugDeviceMessage aMsg)
        {
            _manager.Linear(this,
                Convert.ToDouble((aMsg as FleshlightLaunchFW12Cmd).Speed) / 99,
                Convert.ToDouble((aMsg as FleshlightLaunchFW12Cmd).Position) / 99);
            return new Ok(aMsg.Id);
        }

        private async Task<ButtplugMessage> HandleLinearCmd(ButtplugDeviceMessage aMsg)
        {
            var vis = from x in (aMsg as LinearCmd).Vectors where x.Index == 0 select x;
            if (!vis.Any())
            {
                return new Error("Invalid vibrator index!", Error.ErrorClass.ERROR_DEVICE, aMsg.Id);
            }

            foreach (var vi in vis)
            {
                _manager.Linear2(this, vi.Duration, vi.Position);
            }

            return new Ok(aMsg.Id);
        }
    }
}