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

        public Task<ButtplugMessage> WriteValueAsync(uint aMsgId, uint aCharacteristicIndex, byte[] aValue,
            bool aWriteWithResponse, CancellationToken aToken)
        {
            throw new NotImplementedException();
        }

        public Task<ButtplugMessage> WriteValueAsync(uint aMsgId, ushort[] aValue, bool aWriteWithResponse,
            CancellationToken aToken)
        {
            throw new NotImplementedException();
        }

        public Task<ButtplugMessage> WriteValueAsync(uint aMsgId, uint aCharacteristicIndex, ushort[] aValue,
            bool aWriteWithResponse, CancellationToken aToken)
        {
            throw new NotImplementedException();
        }

        public Task<ButtplugMessage> WriteValueAsync(uint aMsgId, uint[] aValue, bool aWriteWithResponse,
            CancellationToken aToken)
        {
            throw new NotImplementedException();
        }

        public Task<ButtplugMessage> WriteValueAsync(uint aMsgId, uint aCharacteristicIndex, uint[] aValue,
            bool aWriteWithResponse, CancellationToken aToken)
        {
            throw new NotImplementedException();
        }

        public Task<(ButtplugMessage, byte[])> ReadValueAsync(uint aMsgId, CancellationToken aToken)
        {
            throw new NotImplementedException();
        }

        public Task<(ButtplugMessage, byte[])> ReadValueAsync(uint aMsgId, uint aCharacteristicIndex,
            CancellationToken aToken)
        {
            throw new NotImplementedException();
        }

        public Task SubscribeToUpdatesAsync()
        {
            throw new NotImplementedException();
        }

        public Task SubscribeToUpdatesAsync(uint aIndex)
        {
            throw new NotImplementedException();
        }
    }
}