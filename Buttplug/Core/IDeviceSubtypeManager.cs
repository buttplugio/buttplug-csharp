using System;

namespace Buttplug.Core
{
    public interface IDeviceSubtypeManager
    {
        event EventHandler<DeviceAddedEventArgs> DeviceAdded;  
        void StartScanning();
        void StopScanning();
    }
}
