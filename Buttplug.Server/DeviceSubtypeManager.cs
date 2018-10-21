// <copyright file="DeviceSubtypeManager.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Buttplug.Core.Logging;
using JetBrains.Annotations;

namespace Buttplug.Server
{
    public abstract class DeviceSubtypeManager : IDeviceSubtypeManager
    {
        [NotNull]
        protected readonly IButtplugLog BpLogger;
        [NotNull]
        protected readonly IButtplugLogManager LogManager;

        public event EventHandler<DeviceAddedEventArgs> DeviceAdded;

        public event EventHandler<EventArgs> ScanningFinished;

        protected DeviceSubtypeManager([NotNull] IButtplugLogManager aLogManager)
        {
            LogManager = aLogManager;
            BpLogger = aLogManager.GetLogger(GetType());
            BpLogger.Debug($"Setting up Device Manager {GetType().Name}");
        }

        // ReSharper disable once UnusedMember.Global
        protected void InvokeDeviceAdded([NotNull] DeviceAddedEventArgs aEventArgs)
        {
            DeviceAdded?.Invoke(this, aEventArgs);
        }

        // ReSharper disable once UnusedMember.Global
        protected void InvokeScanningFinished()
        {
            ScanningFinished?.Invoke(this, new EventArgs());
        }

        public abstract void StartScanning();

        public abstract void StopScanning();

        public abstract bool IsScanning();
    }
}