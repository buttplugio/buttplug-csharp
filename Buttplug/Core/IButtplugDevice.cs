using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Buttplug.Core
{
    public interface IButtplugDevice
    {
        string Name { get; }
        string Identifier { get; }
        event EventHandler DeviceRemoved;
        IEnumerable<Type> GetAllowedMessageTypes();
        Task<ButtplugMessage> ParseMessage(ButtplugDeviceMessage aMsg);
        Task<ButtplugMessage> Initialize();
    }
}
