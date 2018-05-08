using System;
using JetBrains.Annotations;

namespace Buttplug.Server
{
    public interface IDeviceSubtypeManager
    {
        bool VerboseDeviceLogging { get; set; }

        [CanBeNull]
        event EventHandler<DeviceAddedEventArgs> DeviceAdded;

        [CanBeNull]
        event EventHandler<EventArgs> ScanningFinished;

        void StartScanning();

        void StopScanning();

        bool IsScanning();
    }
}
