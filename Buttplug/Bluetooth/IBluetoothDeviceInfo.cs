using System;
using System.Collections.Generic;
using Buttplug.Core;
using JetBrains.Annotations;

namespace Buttplug.Bluetooth
{
    public interface IBluetoothDeviceInfo
    {
        [NotNull]
        string[] Names { get; }
        [NotNull]
        Guid[] Services { get; }
        [NotNull]
        Guid[] Characteristics { get; }
        
        [NotNull]
        IButtplugDevice CreateDevice([NotNull] IButtplugLogManager aLogManager, [NotNull] IBluetoothDeviceInterface aDeviceInterface);
    }
}