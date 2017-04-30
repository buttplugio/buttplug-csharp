using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Buttplug
{
    abstract class DeviceManager
    {
        public event EventHandler<DeviceAddedEventArgs> DeviceAdded;
        protected void InvokeDeviceAdded(DeviceAddedEventArgs args)
        {
            //Can't invoke this from child classes? Weird.
            DeviceAdded?.Invoke(this, args);
        }
        abstract public void StartScanning();
        abstract public void StopScanning();
    }
}
