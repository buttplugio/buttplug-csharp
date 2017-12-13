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

        public uint Index { get; set; }

        public bool IsConnected
        {
            get
            {
                return !_isDisconnected;
            }
        }

        [CanBeNull]
        public event EventHandler DeviceRemoved;

        [CanBeNull]
        public event EventHandler<MessageReceivedEventArgs> MessageEmitted;

        [NotNull]
        protected readonly IButtplugLog BpLogger;

        [NotNull]
        protected readonly Dictionary<Type, ButtplugDeviceWrapper> MsgFuncs;

        private bool _isDisconnected;

        public class ButtplugDeviceWrapper
        {
            public Func<ButtplugDeviceMessage, Task<ButtplugMessage>> Function;
            public MessageAttributes Attrs;

            public ButtplugDeviceWrapper(Func<ButtplugDeviceMessage, Task<ButtplugMessage>> aFunction,
                                         MessageAttributes aAttrs = null)
            {
                Function = aFunction;
                Attrs = aAttrs ?? new MessageAttributes();
            }
        }

        protected ButtplugDevice([NotNull] IButtplugLogManager aLogManager,
            [NotNull] string aName,
            [NotNull] string aIdentifier)
        {
            BpLogger = aLogManager.GetLogger(GetType());
            MsgFuncs = new Dictionary<Type, ButtplugDeviceWrapper>();
            Name = aName;
            Identifier = aIdentifier;
        }

        public IEnumerable<Type> GetAllowedMessageTypes()
        {
            return MsgFuncs.Keys;
        }

        public MessageAttributes GetMessageAttrs(Type aMsg)
        {
            if (MsgFuncs.TryGetValue(aMsg, out var wrapper))
            {
                return wrapper.Attrs ?? new MessageAttributes();
            }

            return new MessageAttributes();
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
            return await MsgFuncs[aMsg.GetType()].Function.Invoke(aMsg);
        }

        public virtual Task<ButtplugMessage> Initialize()
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            return Task.FromResult<ButtplugMessage>(new Ok(ButtplugConsts.SystemMsgId));
        }

        public abstract void Disconnect();

        protected void EmitMessage(ButtplugMessage aMsg)
        {
            MessageEmitted?.Invoke(this, new MessageReceivedEventArgs(aMsg));
        }
    }
}