using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Buttplug.Messages;

namespace Buttplug.Core
{
    internal abstract class ButtplugDevice : IButtplugDevice
    {
        public string Name { get; }
        public string Identifier { get; }
        public event EventHandler DeviceRemoved;
        protected readonly IButtplugLog BpLogger;
        protected Dictionary<Type, Func<ButtplugDeviceMessage, Task<ButtplugMessage>>> MsgFuncs;
        protected bool IsDisconnected;

        protected ButtplugDevice(IButtplugLogManager aLogManager, 
            string aName,
            string aIdentifier)
        {
            BpLogger = aLogManager.GetLogger(this.GetType());
            MsgFuncs = new Dictionary<Type, Func<ButtplugDeviceMessage, Task<ButtplugMessage>>>();
            Name = aName;
            Identifier = aIdentifier;
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

        public virtual Task<ButtplugMessage> Initialize()
        {
            return Task.FromResult<ButtplugMessage>(new Ok(ButtplugConsts.SYSTEM_MSG_ID));
        }
    }
}