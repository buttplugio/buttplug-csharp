using System;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Messages;

namespace Buttplug.Devices
{
    public interface IButtplugDeviceImpl
    {
        event EventHandler<ButtplugDeviceDataEventArgs> DataReceived;

        event EventHandler DeviceRemoved;

        string Address { get; }

        bool Connected { get; }

        void Disconnect();

        Task<ButtplugMessage> WriteValueAsync(uint aMsgId, byte[] aValue, bool aWriteWithResponse, CancellationToken aToken);

        Task<ButtplugMessage> WriteValueAsync(uint aMsgId, uint aCharacteristicIndex, byte[] aValue, bool aWriteWithResponse, CancellationToken aToken);

        Task<ButtplugMessage> WriteValueAsync(uint aMsgId, ushort[] aValue, bool aWriteWithResponse, CancellationToken aToken);

        Task<ButtplugMessage> WriteValueAsync(uint aMsgId, uint aCharacteristicIndex, ushort[] aValue, bool aWriteWithResponse, CancellationToken aToken);

        Task<ButtplugMessage> WriteValueAsync(uint aMsgId, uint[] aValue, bool aWriteWithResponse, CancellationToken aToken);

        Task<ButtplugMessage> WriteValueAsync(uint aMsgId, uint aCharacteristicIndex, uint[] aValue, bool aWriteWithResponse, CancellationToken aToken);

        Task<(ButtplugMessage, byte[])> ReadValueAsync(uint aMsgId, CancellationToken aToken);

        Task<(ButtplugMessage, byte[])> ReadValueAsync(uint aMsgId, uint aCharacteristicIndex, CancellationToken aToken);

        Task SubscribeToUpdatesAsync();

        Task SubscribeToUpdatesAsync(uint aIndex);
    }
}
