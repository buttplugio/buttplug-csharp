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

        public abstract Task WriteValueAsync(byte[] aValue,
            CancellationToken aToken);

        public abstract Task WriteValueAsync(string aEndpointName, byte[] aValue,
            CancellationToken aToken);

        public abstract Task WriteValueAsync(byte[] aValue, bool aWriteWithResponse,
            CancellationToken aToken);

        public abstract Task WriteValueAsync(string aEndpointName, byte[] aValue,
            bool aWriteWithResponse, CancellationToken aToken);

        public abstract Task<byte[]> ReadValueAsync(CancellationToken aToken);

        public abstract Task<byte[]> ReadValueAsync(string aEndpointName,
            CancellationToken aToken);

        public abstract Task<byte[]> ReadValueAsync(uint aLength, CancellationToken aToken);

        public abstract Task<byte[]> ReadValueAsync(string aEndpointName, uint aLength,
            CancellationToken aToken);

        public abstract Task SubscribeToUpdatesAsync();

        public abstract Task SubscribeToUpdatesAsync(string aEndpointName);

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