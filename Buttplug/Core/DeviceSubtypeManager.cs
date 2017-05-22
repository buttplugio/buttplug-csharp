using System;
using JetBrains.Annotations;

namespace Buttplug.Core
{
    public abstract class DeviceSubtypeManager : IDeviceSubtypeManager
    {
        [NotNull]
        protected readonly IButtplugLog BpLogger;
        [NotNull]
        protected readonly IButtplugLogManager LogManager;

        public event EventHandler<DeviceAddedEventArgs> DeviceAdded;

        protected DeviceSubtypeManager([NotNull] IButtplugLogManager aLogManager)
        {
            LogManager = aLogManager;
            BpLogger = aLogManager.GetLogger(GetType());
            BpLogger.Trace($"Setting up Device Manager {GetType().Name}");
        }

        protected void InvokeDeviceAdded([NotNull] DeviceAddedEventArgs args)
        {
            DeviceAdded?.Invoke(this, args);
        }

        public abstract void StartScanning();
        public abstract void StopScanning();
    }
}