using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Buttplug.Core
{
    public interface IButtplugDevice
    {
        string Name { get; }
        event EventHandler DeviceRemoved;
        IEnumerable<Type> GetAllowedMessageTypes();
        Task<ButtplugMessage> ParseMessage(ButtplugDeviceMessage aMsg);
    }
}
