using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Buttplug.Logging;

namespace Buttplug.Core
{
    internal class ButtplugDevice
    {
        public string Name { get; }
        public event EventHandler DeviceRemoved;
        protected readonly ILog BpLogger = LogProvider.GetCurrentClassLogger();
        protected Dictionary<Type, Func<ButtplugDeviceMessage, Task<ButtplugMessage>>> MsgFuncs;
        protected bool IsDisconnected;

        protected ButtplugDevice(string name)
        {
            MsgFuncs = new Dictionary<Type, Func<ButtplugDeviceMessage, Task<ButtplugMessage>>>();
            Name = name;
        }

        public IEnumerable<Type> GetAllowedMessageTypes()
        {
            return MsgFuncs.Keys;
        }

        protected void InvokeDeviceRemoved()
        {
            IsDisconnected = true;
            DeviceRemoved?.Invoke(this, new EventArgs());
        }

        public IEnumerable<string> GetAllowedMessageTypesAsStrings()
        {
            return from x in MsgFuncs.Keys select x.Name;
        }

        public async Task<ButtplugMessage> ParseMessage(ButtplugDeviceMessage aMsg)
        {
            if (IsDisconnected)
            {
                return ButtplugUtils.LogErrorMsg(aMsg.Id, BpLogger,
                    $"{Name} has disconnected and can no longer process messages.");
            }
            if (!MsgFuncs.ContainsKey(aMsg.GetType()))
            {
                return ButtplugUtils.LogErrorMsg(aMsg.Id, BpLogger,
                    $"{Name} cannot handle message of type {aMsg.GetType().Name}");
            }
            return await MsgFuncs[aMsg.GetType()](aMsg);
        }
    }
}