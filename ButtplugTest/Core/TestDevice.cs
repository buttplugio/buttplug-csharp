using Buttplug.Core;
using Buttplug.Messages;
using LanguageExt;
using System.Threading.Tasks;

namespace ButtplugTest.Core
{
    internal class TestDevice : ButtplugDevice
    {
        public TestDevice(string aName) :
            base(aName)
        {
        }

        public override async Task<Either<Error, ButtplugMessage>> ParseMessage(ButtplugDeviceMessage aMsg)
        {
            return new Ok(aMsg.Id);
        }
    }
}