using Buttplug.Messages;
using LanguageExt;
using NLog;
using System;
using System.Threading.Tasks;

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
        protected Logger BpLogger;

        protected ButtplugDevice(string name)
        {
            BpLogger = LogManager.GetLogger(GetType().FullName);
            Name = name;
        }

        public abstract Task<ButtplugMessage> ParseMessage(ButtplugDeviceMessage aMsg);
    }
}