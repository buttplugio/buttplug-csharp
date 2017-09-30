using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace Buttplug.Server.Managers.SimulatorManager
{
    internal class SimulatedButtplugDevice : ButtplugDevice
    {
        SimulatorManager _manager;

        public SimulatedButtplugDevice(
            SimulatorManager aManager,
            [NotNull] IButtplugLogManager aLogManager,
            [NotNull] DeviceSimulator.PipeMessages.DeviceAdded da)
            : base(aLogManager, da.Name, da.Id)
        {
            _manager = aManager;
            if (da.HasLinear)
            {
                MsgFuncs.Add(typeof(FleshlightLaunchFW12Cmd), HandleFleshlightLaunchFW12Cmd);
            }

            if (da.HasVibrator)
            {
                MsgFuncs.Add(typeof(SingleMotorVibrateCmd), HandleSingleMotorVibrateCmd);
            }

            if (da.HasRotator)
            {
                MsgFuncs.Add(typeof(VorzeA10CycloneCmd), HandleVorzeA10CycloneCmd);
            }

            MsgFuncs.Add(typeof(StopDeviceCmd), HandleStopDeviceCmd);
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
            _manager.Vibrate(this, (aMsg as SingleMotorVibrateCmd).Speed);
            return new Ok(aMsg.Id);
        }

        private async Task<ButtplugMessage> HandleVorzeA10CycloneCmd(ButtplugDeviceMessage aMsg)
        {
            _manager.Rotate(this, (aMsg as VorzeA10CycloneCmd).Speed, (aMsg as VorzeA10CycloneCmd).Clockwise);
            return new Ok(aMsg.Id);
        }

        private async Task<ButtplugMessage> HandleFleshlightLaunchFW12Cmd(ButtplugDeviceMessage aMsg)
        {
            _manager.Linear(this, (aMsg as FleshlightLaunchFW12Cmd).Speed, (aMsg as FleshlightLaunchFW12Cmd).Position);
            return new Ok(aMsg.Id);
        }
    }
}