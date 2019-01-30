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
        public event EventHandler<ButtplugDeviceDataEventArgs> DataReceived;

        public event EventHandler DeviceRemoved;

        public string Address { get; protected set; }

        public bool Connected { get; protected set; }

        public abstract void Disconnect();

        [NotNull]
        protected readonly IButtplugLog BpLogger;

        protected ButtplugDeviceImpl(IButtplugLogManager aLogManager)
        {
            BpLogger = aLogManager.GetLogger(GetType());
        }

        public Task<ButtplugMessage> WriteValueAsync(uint aMsgId, byte[] aValue, bool aWriteWithResponse,
            CancellationToken aToken)
        {
            throw new NotImplementedException();
        }

        public Task<ButtplugMessage> WriteValueAsync(uint aMsgId, string aEndpointName, byte[] aValue,
            bool aWriteWithResponse, CancellationToken aToken)
        {
            throw new NotImplementedException();
        }

        public Task<(ButtplugMessage, byte[])> ReadValueAsync(uint aMsgId, CancellationToken aToken)
        {
            throw new NotImplementedException();
        }

        public Task<(ButtplugMessage, byte[])> ReadValueAsync(uint aMsgId, string aEndpointName,
            CancellationToken aToken)
        {
            throw new NotImplementedException();
        }

        public Task SubscribeToUpdatesAsync()
        {
            throw new NotImplementedException();
        }

        public Task SubscribeToUpdatesAsync(string aEndpointName)
        {
            throw new NotImplementedException();
        }
    }
}