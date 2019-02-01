using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace Buttplug.Devices
{
    /// <summary>
    /// Representation of a device. Presents a unified view of an IButtplugDeviceImpl and
    /// IButtplugProtocol, while also handling the Buttplug specific info like device indexes.
    /// </summary>
    public class ButtplugDevice : IButtplugDevice
    {
        [NotNull]
        protected readonly IButtplugDeviceProtocol _protocol;

        [NotNull]
        protected readonly IButtplugDeviceImpl _device;

        /// <inheritdoc />
        public string Name => _protocol.Name;

        /// <inheritdoc />
        public string Identifier => _device.Address;

        /// <inheritdoc />
        public uint Index { get; set; }

        /// <inheritdoc />
        public bool Connected => _device.Connected;

        /// <inheritdoc />
        [CanBeNull]
        public event EventHandler DeviceRemoved;

        /// <inheritdoc />
        public IEnumerable<Type> AllowedMessageTypes => _protocol.AllowedMessageTypes;

        [NotNull]
        protected readonly IButtplugLog BpLogger;

        private bool _isDisconnected;

        /// <inheritdoc />
        [CanBeNull]
        public event EventHandler<MessageReceivedEventArgs> MessageEmitted;

        /// <summary>
        /// Initializes a new instance of the <see cref="ButtplugDevice"/> class.
        /// </summary>
        /// <param name="aLogManager">The log manager</param>
        /// <param name="aDevice">The device implementation (Bluetooth, USB, etc).</param>
        /// <param name="aProtocol">The device protocol (Lovense, Launch, etc).</param>
        public ButtplugDevice([NotNull] IButtplugLogManager aLogManager,
            [NotNull] IButtplugDeviceProtocol aProtocol,
            [NotNull] IButtplugDeviceImpl aDevice)
        {
            // Protocol can be null if activator construction from type constructor fails
            ButtplugUtils.ArgumentNotNull(aProtocol, nameof(aProtocol));
            _protocol = aProtocol;
            _device = aDevice;
            BpLogger = aLogManager.GetLogger(GetType());
            _device.DeviceRemoved += OnDeviceRemoved;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ButtplugDevice"/> class.
        /// </summary>
        /// <param name="aLogManager">The log manager</param>
        /// <param name="aDevice">The device implementation (Bluetooth, USB, etc).</param>
        /// <param name="aProtocolType">A Type for a protocol, which we will create an instance of</param>
        public ButtplugDevice([NotNull] IButtplugLogManager aLogManager,
            [NotNull] Type aProtocolType,
            [NotNull] IButtplugDeviceImpl aDevice)
        : this(aLogManager,
            // A lot of trust happening in the structure of protocol constructors here.
            // todo should probably document the many ways this can throw.
            (IButtplugDeviceProtocol)Activator.CreateInstance(aProtocolType, aLogManager, aDevice),
            aDevice)
        {
        }

        public static ButtplugDevice Create<T>(IButtplugLogManager aLogManager,
            IButtplugDeviceImpl aDevice,
            Func<IButtplugLogManager, IButtplugDeviceImpl, T> aProtocolCreationFunc)
        where T : IButtplugDeviceProtocol, new()
        {
            var p = aProtocolCreationFunc(aLogManager, aDevice);
            return new ButtplugDevice(aLogManager, p, aDevice);
        }

        /// <inheritdoc />
        public MessageAttributes GetMessageAttrs(Type aMsg)
        {
            return _protocol.GetMessageAttrs(aMsg);
        }

        /// <inheritdoc />
        public async Task<ButtplugMessage> ParseMessageAsync([NotNull] ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            if (_isDisconnected)
            {
                throw new ButtplugDeviceException(BpLogger, $"{Name} has disconnected and can no longer process messages.", aMsg.Id);
            }

            return await _protocol.ParseMessageAsync(aMsg, aToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public virtual Task<ButtplugMessage> InitializeAsync(CancellationToken aToken)
        {
            return _protocol.InitializeAsync(aToken);
        }

        /// <inheritdoc />
        public void Disconnect()
        {
            _device.Disconnect();
        }

        protected void OnDeviceRemoved(object aObj, EventArgs aArgs)
        {
            InvokeDeviceRemoved();
        }

        /// <summary>
        /// Invokes the DeviceRemoved event handler.
        /// Required to disconnect devices from the lower levels.
        /// </summary>
        protected void InvokeDeviceRemoved()
        {
            _isDisconnected = true;
            DeviceRemoved?.Invoke(this, new EventArgs());
        }
    }
}