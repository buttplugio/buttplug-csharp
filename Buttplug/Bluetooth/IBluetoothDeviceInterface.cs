using System;
using System.Threading.Tasks;
using Buttplug.Core;

namespace Buttplug.Bluetooth
{
    public interface IBluetoothDeviceInterface
    {
        string Name { get; }
        Task<ButtplugMessage> WriteValue(uint aMsgId, uint aCharacteristicIndex, byte[] aValue, bool aWriteWithResponse = false);
        ulong GetAddress();
        event EventHandler DeviceRemoved;
    }
}
