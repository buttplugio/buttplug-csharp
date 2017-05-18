using System;
using System.Collections.Generic;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Buttplug.Core;

namespace ButtplugUWPBluetoothManager.Core
{
    internal interface IBluetoothDeviceInfo
    {
        string[] Names { get; }
        Guid[] Services { get; }
        Guid[] Characteristics { get; }

        ButtplugBluetoothDevice CreateDevice(IButtplugLogManager aLogManager, BluetoothLEDevice aDevice, Dictionary<Guid, GattCharacteristic> aCharacteristics);
    }
}