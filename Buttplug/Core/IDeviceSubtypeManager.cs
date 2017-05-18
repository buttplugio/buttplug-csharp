using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Buttplug.Core
{
    public interface IDeviceSubtypeManager
    {
        event EventHandler<DeviceAddedEventArgs> DeviceAdded;  
        void StartScanning();
        void StopScanning();
    }
}
