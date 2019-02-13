using System;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Logging;
using JetBrains.Annotations;

namespace Buttplug.Devices
{
    public abstract class ButtplugDeviceImpl : IButtplugDeviceImpl
    {
        public event EventHandler<ButtplugDeviceDataEventArgs> DataReceived;

        public event EventHandler DeviceRemoved;

        public string Name { get; protected set; }

        public string Address { get; protected set; }

        /// <summary>
        /// Connected is abstract, as implementing classes may use it as a computed property.
        /// </summary>
        public abstract bool Connected { get; }

        [NotNull]
        protected readonly IButtplugLog BpLogger;

        protected ButtplugDeviceImpl(IButtplugLogManager aLogManager)
        {
            BpLogger = aLogManager.GetLogger(GetType());
        }

        public Task WriteValueAsync(byte[] aValue,
            CancellationToken aToken = default(CancellationToken))
        {
            return WriteValueAsyncInternal(aValue, new ButtplugDeviceWriteOptions(), aToken);
        }

        public Task WriteValueAsync(byte[] aValue,
            ButtplugDeviceWriteOptions aOptions = default(ButtplugDeviceWriteOptions),
            CancellationToken aToken = default(CancellationToken))
        {
            return WriteValueAsyncInternal(aValue, aOptions ?? new ButtplugDeviceWriteOptions(), aToken);
        }

        public abstract Task WriteValueAsyncInternal(byte[] aValue,
            ButtplugDeviceWriteOptions aOptions,
            CancellationToken aToken = default(CancellationToken));

        public Task<byte[]> ReadValueAsync(CancellationToken aToken = default(CancellationToken))
        {
            return ReadValueAsync(new ButtplugDeviceReadOptions(), aToken);
        }

        public Task<byte[]> ReadValueAsync(ButtplugDeviceReadOptions aOptions = default(ButtplugDeviceReadOptions),
            CancellationToken aToken = default(CancellationToken))
        {
            return ReadValueAsyncInternal(aOptions ?? new ButtplugDeviceReadOptions(), aToken);
        }

        public abstract Task<byte[]> ReadValueAsyncInternal(ButtplugDeviceReadOptions aOptions,
            CancellationToken aToken = default(CancellationToken));

        public Task SubscribeToUpdatesAsync(
            ButtplugDeviceReadOptions aOptions = default(ButtplugDeviceReadOptions))
        {
            return SubscribeToUpdatesAsyncInternal(aOptions ?? new ButtplugDeviceReadOptions());
        }

        public abstract Task SubscribeToUpdatesAsyncInternal(ButtplugDeviceReadOptions aOptions);

        public abstract void Disconnect();

        protected void InvokeDeviceRemoved()
        {
            DeviceRemoved?.Invoke(this, EventArgs.Empty);
        }

        protected void InvokeDataReceived(ButtplugDeviceDataEventArgs aArgs)
        {
            DataReceived?.Invoke(this, aArgs);
        }
    }
}
