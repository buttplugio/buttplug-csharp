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

        IButtplugDevice CreateDevice(IButtplugLogManager aLogManager, IBluetoothDeviceInterface aDeviceInterface);
    }
}