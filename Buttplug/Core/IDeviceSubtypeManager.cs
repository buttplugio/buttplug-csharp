using System;
using JetBrains.Annotations;

namespace Buttplug.Core
{
    public interface IDeviceSubtypeManager
    {
        [CanBeNull]
        event EventHandler<DeviceAddedEventArgs> DeviceAdded;  
        void StartScanning();
        void StopScanning();
    }
}
