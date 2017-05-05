using System;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Buttplug.Core
{
    internal interface IBluetoothDeviceInfo
    {
        string[] Names { get; }
        Guid[] Services { get; }
        Guid[] Characteristics { get; }
        ButtplugBluetoothDevice CreateDevice(BluetoothLEDevice aDevice, GattCharacteristic[] aCharacteristics);
    }

}
