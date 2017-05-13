using NLog;
using System;
using System.Linq;
using System.Collections.Generic;
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
        protected Dictionary<Type, Func<ButtplugDeviceMessage, Task<ButtplugMessage>>> MsgFuncs;

        protected ButtplugDevice(string name)
        {
            MsgFuncs = new Dictionary<Type, Func<ButtplugDeviceMessage, Task<ButtplugMessage>>>();
            BpLogger = LogManager.GetLogger(GetType().FullName);
            Name = name;
        }

        public IEnumerable<Type> GetAllowedMessageTypes()
        {
            return MsgFuncs.Keys;
        }

        public IEnumerable<string> GetAllowedMessageTypesAsStrings()
        {
            return from x in MsgFuncs.Keys select x.Name;
        }

        public async Task<ButtplugMessage> ParseMessage(ButtplugDeviceMessage aMsg)
        {
            if (!MsgFuncs.ContainsKey(aMsg.GetType()))
            {
                return ButtplugUtils.LogAndError(aMsg.Id, BpLogger, LogLevel.Error,
                    $"{Name} cannot handle message of type {aMsg.GetType().Name}");
            }
            return await MsgFuncs[aMsg.GetType()](aMsg);
        }
    }
}