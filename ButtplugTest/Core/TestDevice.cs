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
        }

        public override async Task<ButtplugMessage> ParseMessage(ButtplugDeviceMessage aMsg)
        {
            switch (aMsg)
            {
                case SingleMotorVibrateCmd m:
                    BpLogger.Trace("Test Device got SingleMotorVibrateMessage");
                    return new Ok(aMsg.Id);
            }

            return ButtplugUtils.LogAndError(aMsg.Id, BpLogger, LogLevel.Error, $"{Name} cannot handle message of type {aMsg.GetType().Name}");
        }
    }
}