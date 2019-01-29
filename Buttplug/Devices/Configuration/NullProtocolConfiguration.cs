using System;
using System.Collections.Generic;
using System.Text;

namespace Buttplug.Devices.Configuration
{
    class NullProtocolConfiguration : IProtocolConfiguration
    {
        public NullProtocolConfiguration()
        {
        }

        public bool Matches(IProtocolConfiguration aConfig)
        {
            return false;
        }
    }
}
