using System;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;

namespace Buttplug.Server.Test
{
    internal class TestDevice : ButtplugDevice
    {
        public TestDevice(ButtplugLogManager aLogManager, string aName)
            : base(aLogManager, aName, "Test")
        {
            MsgFuncs.Add(typeof(SingleMotorVibrateCmd), new ButtplugDeviceWrapper(HandleSingleMotorVibrateCmd));
        }

        private Task<ButtplugMessage> HandleSingleMotorVibrateCmd(ButtplugDeviceMessage aMsg)
        {
            // BpLogger.Trace("Test Device got SingleMotorVibrateMessage");
            return Task.FromResult<ButtplugMessage>(new Ok(aMsg.Id));
        }

        public void RemoveDevice()
        {
            InvokeDeviceRemoved();
        }

        public override void Disconnect()
        {
            RemoveDevice();
        }
    }
}
