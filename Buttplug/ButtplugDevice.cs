using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using LanguageExt;

namespace Buttplug
{
    public interface IButtplugDevice
    {
        String Name { get; }
        bool ParseMessage(ButtplugMessage msg);
        bool Connect();
        bool Disconnect();

    }
}
