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

        ulong GetAddress();

        event EventHandler DeviceRemoved;

        void Disconnect();
    }
}
