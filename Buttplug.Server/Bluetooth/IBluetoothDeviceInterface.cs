using System;
using System.Threading.Tasks;
using Buttplug.Core;

namespace Buttplug.Server.Bluetooth
{
    public interface IBluetoothDeviceInterface
    {
        string Name { get; }

        Task<ButtplugMessage> WriteValue(uint aMsgId, byte[] aValue, bool aWriteWithResponse = false);

        Task<ButtplugMessage> WriteValue(uint aMsgId, uint aCharactieristicIndex, byte[] aValue, bool aWriteWithResponse = false);

        // TODO If Unity requires < 4.7, this may need to be changed to use out params instead of tuple returns.
        Task<(ButtplugMessage, byte[])> ReadValue(uint aMsgId);

        // TODO If Unity requires < 4.7, this may need to be changed to use out params instead of tuple returns.
        Task<(ButtplugMessage, byte[])> ReadValue(uint aMsgId, uint aCharacteristicIndex);

        Task SubscribeToUpdates();

        ulong GetAddress();

        event EventHandler DeviceRemoved;

        void Disconnect();
    }
}
