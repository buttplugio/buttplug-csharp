using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Buttplug
{
    public class DeviceAddedEventArgs : EventArgs
    {
        public DeviceAddedEventArgs(IButtplugDevice d)
        {
            this.device = d;
        }

        public IButtplugDevice device;
    }

    abstract class IDeviceManager
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

    public class ButtplugService
    {
        List<IDeviceManager> Managers;
        public event EventHandler<DeviceAddedEventArgs> DeviceAdded;
        // TODO Should I just make StartScanning async across device managers?
        public event EventHandler FinishedScanning;

        public ButtplugService()
        {
            Managers = new List<IDeviceManager>();
            Managers.Add(new BluetoothManager());
            Managers.Add(new GamepadManager());
            Managers.ForEach(m => m.DeviceAdded += DeviceAddedHandler);
        }

        public void DeviceAddedHandler(object o, DeviceAddedEventArgs e)
        {
            DeviceAdded?.Invoke(this, e);
        }

        public void StartScanning()
        {
            Managers.ForEach(m => m.StartScanning());
        }

        public void StopScanning()
        {
            Managers.ForEach(m => m.StopScanning());
        }
    }
}
