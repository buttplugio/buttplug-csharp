using System;
using System.Collections.Generic;
using Buttplug.Core;
using JetBrains.Annotations;

namespace Buttplug.Server.Bluetooth
{
    public interface IBluetoothDeviceInfo
    {
        [NotNull]
        string[] Names { get; }

        [NotNull]
        string[] NamePrefixes { get; }

        [NotNull]
        Guid[] Services { get; }

        [NotNull]
        Dictionary<uint, Guid> Characteristics { get; }

        [NotNull]
        IButtplugDevice CreateDevice([NotNull] IButtplugLogManager aLogManager, [NotNull] IBluetoothDeviceInterface aDeviceInterface);
    }
}