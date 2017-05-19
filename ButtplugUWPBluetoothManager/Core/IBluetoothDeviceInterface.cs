using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Buttplug.Core;

namespace ButtplugUWPBluetoothManager.Core
{
    interface IBluetoothDeviceInterface
    {
        string Name { get; }
        Task<ButtplugMessage> WriteValue(uint aMsgId, uint aCharacteristicIndex, byte[] aValue);
        Task<byte[]> ReadValue(uint aCharacteristicIndex);
        Task<ButtplugMessage> Subscribe(uint aMsgId, uint aCharacertisticIndex);
        Task<ButtplugMessage> Unsubscribe(uint aMsgId, uint aCharacertisticIndex);
        ulong GetAddress();
        event EventHandler DeviceRemoved;
    }
}
