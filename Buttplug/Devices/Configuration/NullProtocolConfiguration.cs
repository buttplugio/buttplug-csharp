using System;
using System.Collections.Generic;
using System.Text;

namespace Buttplug.Devices.Configuration
{
    class NullProtocolConfiguration : IProtocolConfiguration
    {
        public NullProtocolConfiguration(Type aType)
        {
        }

        public bool Matches(IProtocolConfiguration aConfig)
        {
            return false;
        }
    }
}
