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
            [NotNull] string aName,
            [NotNull] string aIdentifier)
            : base(aLogManager, aName, aIdentifier)
        {
            _manager = aManager;
            MsgFuncs.Add(typeof(SingleMotorVibrateCmd), HandleSingleMotorVibrateCmd);
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
    }
}