// <copyright file="IDeviceSubtypeManager.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using JetBrains.Annotations;

namespace Buttplug.Server
{
    public interface IDeviceSubtypeManager
    {
        [CanBeNull]
        event EventHandler<DeviceAddedEventArgs> DeviceAdded;

        [CanBeNull]
        event EventHandler<EventArgs> ScanningFinished;

        void StartScanning();

        void StopScanning();

        bool IsScanning();
    }
}
