using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Buttplug.Logging;

namespace Buttplug.Core
{
    internal class ButtplugDevice : IButtplugDevice
    {
        public string Name { get; }
        public event EventHandler DeviceRemoved;
        protected readonly IButtplugLog BpLogger;
        protected Dictionary<Type, Func<ButtplugDeviceMessage, Task<ButtplugMessage>>> MsgFuncs;
        protected bool IsDisconnected;

        protected ButtplugDevice(IButtplugLogManager aLogManager, string name)
        {
            BpLogger = aLogManager.GetLogger(this.GetType());
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

        public async Task<ButtplugMessage> ParseMessage(ButtplugDeviceMessage aMsg)
        {
            if (IsDisconnected)
            {
                return BpLogger.LogErrorMsg(aMsg.Id,
                    $"{Name} has disconnected and can no longer process messages.");
            }
            if (!MsgFuncs.ContainsKey(aMsg.GetType()))
            {
                return BpLogger.LogErrorMsg(aMsg.Id,
                    $"{Name} cannot handle message of type {aMsg.GetType().Name}");
            }
            return await MsgFuncs[aMsg.GetType()](aMsg);
        }
    }
}