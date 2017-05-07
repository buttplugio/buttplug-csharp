using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Buttplug.Core;
using Buttplug.Messages;

namespace ButtplugTest.Core
{
    public class ButtplugServerTests
    {
        [Fact]
        async void RejectOutgoingOnlyMessage()
        {
            Assert.False(await new ButtplugService().SendMessage(new Error("Error")));
        }
    }
}
