// <copyright file="IBluetoothDeviceInfo.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using Buttplug.Core.Devices;
using Buttplug.Core.Logging;
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