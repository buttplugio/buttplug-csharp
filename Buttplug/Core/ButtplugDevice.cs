using System;
using System.Threading.Tasks;
using NLog;
using LanguageExt;
using Buttplug.Messages;

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

        public abstract Task<Either<Error, IButtplugMessage>> ParseMessage(IButtplugDeviceMessage aMsg);
    }
}
