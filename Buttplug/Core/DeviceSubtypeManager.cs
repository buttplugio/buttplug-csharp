using System;

namespace Buttplug.Core
{
    public abstract class DeviceSubtypeManager : IDeviceSubtypeManager
    {
        protected readonly IButtplugLog BpLogger;
        protected readonly IButtplugLogManager LogManager;

        public event EventHandler<DeviceAddedEventArgs> DeviceAdded;

        protected DeviceSubtypeManager(IButtplugLogManager aLogManager)
        {
            LogManager = aLogManager;
            BpLogger = aLogManager.GetLogger(GetType());
            BpLogger.Trace($"Setting up Device Manager {GetType().Name}");
        }

        protected void InvokeDeviceAdded(DeviceAddedEventArgs args)
        {
            DeviceAdded?.Invoke(this, args);
        }

        public abstract void StartScanning();
        public abstract void StopScanning();
    }
}