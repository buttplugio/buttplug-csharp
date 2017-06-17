using System;
using System.Threading.Tasks;
using Buttplug.Core;

namespace Buttplug.Bluetooth
{
    public interface IBluetoothDeviceInterface
    {
        string Name { get; }
        Task<ButtplugMessage> WriteValue(uint aMsgId, uint aCharacteristicIndex, byte[] aValue, bool aWriteWithResponse = false);
        Task<byte[]> ReadValue(uint aCharacteristicIndex);
        Task<ButtplugMessage> Subscribe(uint aMsgId, uint aCharacertisticIndex);
        Task<ButtplugMessage> Unsubscribe(uint aMsgId, uint aCharacertisticIndex);
        ulong GetAddress();
        event EventHandler DeviceRemoved;
    }
}
