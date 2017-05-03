using System;
using NLog;

namespace Buttplug
{
    abstract class DeviceManager
    {
        protected Logger BPLogger;
        public event EventHandler<DeviceAddedEventArgs> DeviceAdded;
        protected DeviceManager()
        {
            BPLogger = LogManager.GetLogger("Buttplug");
            BPLogger.Trace($"Setting up Device Manager {this.GetType().Name}");
        }
        protected void InvokeDeviceAdded(DeviceAddedEventArgs args)
        {
            //Can't invoke this from child classes? Weird.
            DeviceAdded?.Invoke(this, args);
        }
        abstract public void StartScanning();
        abstract public void StopScanning();
    }
}
