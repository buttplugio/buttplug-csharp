using System;
using System.Threading.Tasks;
using NLog;

namespace Buttplug
{
    public class DeviceAddedEventArgs : EventArgs
    {
        public ButtplugDevice Device { get; }
        public DeviceAddedEventArgs(ButtplugDevice d)
        {
            this.Device = d;
        }
    }

    public abstract class ButtplugDevice
    {
        public String Name { get; }
        public abstract Task<bool> ParseMessage(IButtplugDeviceMessage aMsg);
        protected Logger BPLogger;

        protected ButtplugDevice(String name)
        {
            BPLogger = LogManager.GetLogger("Buttplug");
            Name = name;
        }
    }
}
