using System;
using Buttplug.Logging;

namespace Buttplug.Core
{
    internal abstract class DeviceManager
    {
        protected readonly ILog BpLogger;

        public event EventHandler<DeviceAddedEventArgs> DeviceAdded;

        protected DeviceManager()
        {
            BpLogger = LogProvider.GetCurrentClassLogger();
            BpLogger.Trace($"Setting up Device Manager {GetType().Name}");
        }

        protected void InvokeDeviceAdded(DeviceAddedEventArgs args)
        {
            //Can't invoke this from child classes? Weird.
            DeviceAdded?.Invoke(this, args);
        }

        public abstract void StartScanning();

        public abstract void StopScanning();
    }
}