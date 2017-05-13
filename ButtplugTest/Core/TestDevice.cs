using System;
using Buttplug.Core;
using Buttplug.Messages;
using LanguageExt;
using System.Threading.Tasks;
using NLog;

namespace ButtplugTest.Core
{
    internal class TestDevice : ButtplugDevice
    {
        public TestDevice(string aName) :
            base(aName)
        {
            MsgFuncs.Add(typeof(SingleMotorVibrateCmd), HandleSingleMotorVibrateCmd);
        }

        public async Task<ButtplugMessage> HandleSingleMotorVibrateCmd(ButtplugDeviceMessage aMsg)
        {
            BpLogger.Trace("Test Device got SingleMotorVibrateMessage");
            return new Ok(aMsg.Id);
        }
    }
}