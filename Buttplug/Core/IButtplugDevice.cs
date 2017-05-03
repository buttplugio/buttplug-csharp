using System;
using System.Threading.Tasks;

namespace Buttplug
{
    public interface IButtplugDevice
    {
        String Name { get; }
        Task<bool> ParseMessage(IButtplugDeviceMessage aMsg);
    }
}
