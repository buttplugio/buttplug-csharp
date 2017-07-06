using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Messages;

namespace ButtplugTest.Core
{
    internal class TestDevice : ButtplugDevice
    {
        public TestDevice(ButtplugLogManager aLogManager, string aName)
            : base(aLogManager, aName, "Test")
        {
            MsgFuncs.Add(typeof(SingleMotorVibrateCmd), HandleSingleMotorVibrateCmd);
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
    }
}