using System;
using System.Threading.Tasks;
using NLog;

namespace Buttplug.Core
{
    public class DeviceAddedEventArgs : EventArgs
    {
        public ButtplugDevice Device { get; }
        public DeviceAddedEventArgs(ButtplugDevice d)
        {
            Device = d;
        }
    }

    public abstract class ButtplugDevice
    {
        public string Name { get; }
        public abstract Task<bool> ParseMessage(IButtplugDeviceMessage aMsg);
        protected Logger BpLogger;

        protected ButtplugDevice(string name)
        {
            BpLogger = LogManager.GetLogger(GetType().FullName);
            Name = name;
        }
    }
}
