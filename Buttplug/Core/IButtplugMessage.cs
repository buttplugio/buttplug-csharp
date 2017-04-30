using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Buttplug
{
    public interface IButtplugMessage
    {
    }

    public interface IButtplugDeviceMessage : IButtplugMessage
    {
        UInt32 DeviceIndex { get; }
    }
}
