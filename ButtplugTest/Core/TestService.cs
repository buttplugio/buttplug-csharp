using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buttplug.Core;

namespace ButtplugTest.Core
{
    internal class TestService : ButtplugService
    {
        public TestService(TestDeviceManager mgr)
        {
            AddManager(mgr);
        }
    }
}
