using System;
using System.Collections.Generic;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Buttplug.Core
{
    internal interface IBluetoothDeviceInfo
    {
        string[] Names { get; }
        Guid[] Services { get; }
        Guid[] Characteristics { get; }

        ButtplugBluetoothDevice CreateDevice(BluetoothLEDevice aDevice, Dictionary<Guid, GattCharacteristic> aCharacteristics);
    }
}