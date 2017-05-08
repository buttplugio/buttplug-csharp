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

        public override async Task<Either<Error, IButtplugMessage>> ParseMessage(IButtplugDeviceMessage aMsg)
        {
            return new Ok();
        }
    }
}