using Buttplug.Core.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Logging;
using JetBrains.Annotations;

namespace Buttplug.Devices
{
    public abstract class ButtplugDeviceImpl : IButtplugDeviceImpl
    {
        public abstract event EventHandler<ButtplugDeviceDataEventArgs> DataReceived;

        public abstract event EventHandler DeviceRemoved;

        public abstract string Name { get; }

        public abstract string Address { get; }

        public abstract bool Connected { get; }

        public abstract void Disconnect();

        [NotNull]
        protected readonly IButtplugLog BpLogger;

        protected ButtplugDeviceImpl(IButtplugLogManager aLogManager)
        {
            BpLogger = aLogManager.GetLogger(GetType());
        }

        public abstract Task<ButtplugMessage> WriteValueAsync(uint aMsgId, byte[] aValue, bool aWriteWithResponse,
            CancellationToken aToken);

        public abstract Task<ButtplugMessage> WriteValueAsync(uint aMsgId, string aEndpointName, byte[] aValue,
            bool aWriteWithResponse, CancellationToken aToken);

        public abstract Task<(ButtplugMessage, byte[])> ReadValueAsync(uint aMsgId, CancellationToken aToken);

        public abstract Task<(ButtplugMessage, byte[])> ReadValueAsync(uint aMsgId, string aEndpointName,
            CancellationToken aToken);

        public abstract Task SubscribeToUpdatesAsync();

        public abstract Task SubscribeToUpdatesAsync(string aEndpointName);
    }
}