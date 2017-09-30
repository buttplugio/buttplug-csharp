using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Buttplug.Core.Messages;
using JetBrains.Annotations;
using static Buttplug.Core.Messages.Error;

namespace Buttplug.Core
{
    public abstract class ButtplugDevice : IButtplugDevice
    {
        public string Name { get; }

        public string Identifier { get; }

        public bool IsConnected
        {
            get
            {
                return !_isDisconnected;
            }
        }

        [CanBeNull]
        public event EventHandler DeviceRemoved;

        [NotNull]
        protected readonly IButtplugLog BpLogger;
        [NotNull]
        protected readonly Dictionary<Type, Func<ButtplugDeviceMessage, Task<ButtplugMessage>>> MsgFuncs;
        private bool _isDisconnected;

        protected ButtplugDevice([NotNull] IButtplugLogManager aLogManager,
            [NotNull] string aName,
            [NotNull] string aIdentifier)
        {
            BpLogger = aLogManager.GetLogger(GetType());
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
            _isDisconnected = true;
            DeviceRemoved?.Invoke(this, new EventArgs());
        }

        public async Task<ButtplugMessage> ParseMessage([NotNull] ButtplugDeviceMessage aMsg)
        {
            if (_isDisconnected)
            {
                return BpLogger.LogErrorMsg(aMsg.Id, ErrorClass.ERROR_DEVICE,
                    $"{Name} has disconnected and can no longer process messages.");
            }

            if (!MsgFuncs.ContainsKey(aMsg.GetType()))
            {
                return BpLogger.LogErrorMsg(aMsg.Id, ErrorClass.ERROR_DEVICE,
                    $"{Name} cannot handle message of type {aMsg.GetType().Name}");
            }

            // We just checked whether the key exists above, so we're ok.
            // ReSharper disable once PossibleNullReferenceException
            return await MsgFuncs[aMsg.GetType()].Invoke(aMsg);
        }

        public virtual Task<ButtplugMessage> Initialize()
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            return Task.FromResult<ButtplugMessage>(new Ok(ButtplugConsts.SystemMsgId));
        }

        public abstract void Disconnect();
    }
}