using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace Buttplug.Core.Devices
{
    /// <summary>
    /// Abstract representation of a device
    /// </summary>
    public abstract class ButtplugDevice : IButtplugDevice
    {
        /// <inheritdoc />
        public string Name { get; protected set; }

        /// <inheritdoc />
        public string Identifier { get; }

        /// <inheritdoc />
        public uint Index { get; set; }

        /// <inheritdoc />
        public bool IsConnected => !_isDisconnected;

        /// <inheritdoc />
        [CanBeNull]
        public event EventHandler DeviceRemoved;

        /// <inheritdoc />
        public IEnumerable<Type> AllowedMessageTypes => MsgFuncs.Keys;

        /// <summary>
        /// Gets the logger
        /// </summary>
        [NotNull]
        protected readonly IButtplugLog BpLogger;

        /// <summary>
        /// Gets the message handler functions.
        /// </summary>
        /// <remarks>
        /// This is kept private so that we can regulate keys being added. Otherwise it's easy to
        /// accidentally copy/paste duplicate keys when adding new functions, and that is hell to debug.
        /// </remarks>
        [NotNull]
        private readonly Dictionary<Type, (Func<ButtplugDeviceMessage, CancellationToken, Task<ButtplugMessage>> Function, MessageAttributes Attrs)> MsgFuncs;

        private bool _isDisconnected;

        /// <inheritdoc />
        [CanBeNull]
        public event EventHandler<MessageReceivedEventArgs> MessageEmitted;

        /// <summary>
        /// Initializes a new instance of the <see cref="ButtplugDevice"/> class.
        /// </summary>
        /// <param name="aLogManager">The log manager</param>
        /// <param name="aName">The device name</param>
        /// <param name="aIdentifier">The device identifier</param>
        protected ButtplugDevice([NotNull] IButtplugLogManager aLogManager,
            [NotNull] string aName,
            [NotNull] string aIdentifier)
        {
            BpLogger = aLogManager.GetLogger(GetType());
            MsgFuncs =
                new Dictionary<Type, (Func<ButtplugDeviceMessage, CancellationToken, Task<ButtplugMessage>> Function,
                    MessageAttributes Attrs)>();
            Name = aName;
            Identifier = aIdentifier;
        }

        /// <inheritdoc />
        public MessageAttributes GetMessageAttrs(Type aMsg)
        {
            if (MsgFuncs.TryGetValue(aMsg, out var wrapper))
            {
                return wrapper.Attrs ?? new MessageAttributes();
            }

            return new MessageAttributes();
        }

        /// <inheritdoc />
        public async Task<ButtplugMessage> ParseMessageAsync([NotNull] ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            if (_isDisconnected)
            {
                throw new ButtplugDeviceException(BpLogger, $"{Name} has disconnected and can no longer process messages.", aMsg.Id);
            }

            if (!MsgFuncs.ContainsKey(aMsg.GetType()))
            {
                throw new ButtplugDeviceException(BpLogger, $"{Name} cannot handle message of type {aMsg.GetType().Name}", aMsg.Id);
            }

            // We just checked whether the key exists above, so we're ok.
            // ReSharper disable once PossibleNullReferenceException
            return await MsgFuncs[aMsg.GetType()].Function.Invoke(aMsg, aToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public virtual Task<ButtplugMessage> InitializeAsync(CancellationToken aToken)
        {
            return Task.FromResult<ButtplugMessage>(new Ok(ButtplugConsts.SystemMsgId));
        }

        /// <inheritdoc />
        public abstract void Disconnect();

        /// <summary>
        /// Invokes the DeviceRemoved event handler.
        /// Required to disconnect devices from the lower levels.
        /// </summary>
        protected void InvokeDeviceRemoved()
        {
            _isDisconnected = true;
            DeviceRemoved?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Used by deriving classes to add ButtplugDeviceMessage handlers to the handler map. Checks
        /// to make sure duplicate entries are not being added.
        /// </summary>
        /// <remarks>
        /// Having this as a generic with a type constraint rather than a type parameter means we get
        /// type checking at compile/roslyn time.
        /// </remarks>
        /// <typeparam name="T">ButtplugDeviceMsg deriving type</typeparam>
        /// <param name="aFunction">Handler for the message type</param>
        /// <param name="aAttrs">MessageAttribute parameters, assuming the message type has any.</param>
        protected void AddMessageHandler<T>(Func<ButtplugDeviceMessage, CancellationToken, Task<ButtplugMessage>> aFunction,
            MessageAttributes aAttrs = null) where T : ButtplugDeviceMessage
        {
            MsgFuncs.Add(typeof(T), (aFunction, aAttrs ?? new MessageAttributes()));
        }

        protected T CheckMessageHandler<T>(ButtplugDeviceMessage aMsg)
            where T : ButtplugDeviceMessage
        {
            return (aMsg is T cmdMsg) ? cmdMsg : throw new ButtplugDeviceException(BpLogger, $"Wrong handler for message type {aMsg.GetType()}", aMsg.Id);
        }

        private void CheckGenericSubcommandList<T>(ButtplugDeviceMessage aMsg, IEnumerable<T> aCmdList, uint aLimitValue)
        where T : GenericMessageSubcommand
        {
            if (!aCmdList.Any() || aCmdList.Count() > aLimitValue)
            {
                if (aLimitValue == 1)
                {
                    throw new ButtplugDeviceException(BpLogger, $"{aMsg.GetType().Name} requires 1 subcommand for this device, {aCmdList.Count()} present.", aMsg.Id);
                }
                throw new ButtplugDeviceException(BpLogger, $"{aMsg.GetType().Name} requires between 1 and {aLimitValue} subcommands for this device, {aCmdList.Count()} present.", aMsg.Id);
            }
            foreach (var cmd in aCmdList)
            {
                if (cmd.Index >= aLimitValue)
                {
                    throw new ButtplugDeviceException(BpLogger, $"Index {cmd.Index} is out of bounds for {aMsg.GetType().Name} for this device.", aMsg.Id);
                }
            }
        }

        protected T CheckGenericMessageHandler<T>(ButtplugDeviceMessage aMsg, uint aLimitValue)
            where T : ButtplugDeviceMessage
        {
            var actualMsg = CheckMessageHandler<T>(aMsg);

            // Can't seem to pattern match this, so big ol' if/else chain it is. :(
            if (typeof(T) == typeof(VibrateCmd))
            {
                var cmdMsg = actualMsg as VibrateCmd;
                CheckGenericSubcommandList(cmdMsg, cmdMsg.Speeds, aLimitValue);
                return actualMsg;
            }
            if (typeof(T) == typeof(RotateCmd))
            {
                var cmdMsg = actualMsg as RotateCmd;
                CheckGenericSubcommandList(cmdMsg, cmdMsg.Rotations, aLimitValue);
                return actualMsg;
            }
            if (typeof(T) == typeof(LinearCmd))
            {
                var cmdMsg = actualMsg as LinearCmd;
                CheckGenericSubcommandList(cmdMsg, cmdMsg.Vectors, aLimitValue);
                return actualMsg;
            }
            throw new ButtplugMessageException("CheckGenericMessageHandler only works with generic (VibrateCmd/RotateCmd/etc) messages.", aMsg.Id);
        }
    }
}