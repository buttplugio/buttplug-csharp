using System;
using Buttplug.Core;

namespace Buttplug.Bluetooth
{
    public interface IBluetoothDeviceInfo
    {
        string[] Names { get; }
        Guid[] Services { get; }
        Guid[] Characteristics { get; }

        IButtplugDevice CreateDevice(IButtplugLogManager aLogManager, IBluetoothDeviceInterface aDeviceInterface);
    }
}