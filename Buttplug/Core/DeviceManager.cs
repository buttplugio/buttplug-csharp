using System;
using NLog;

namespace Buttplug.Core
{
    internal abstract class DeviceManager
    {
        protected Logger BpLogger;
        public event EventHandler<DeviceAddedEventArgs> DeviceAdded;

        protected DeviceManager()
        {
            BpLogger = LogManager.GetLogger("Buttplug");
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
