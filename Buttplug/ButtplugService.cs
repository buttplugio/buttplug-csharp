using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Buttplug
{
    public class ButtplugService
    {
        private BluetoothManager mBluetooth;
        public event EventHandler<DeviceFoundEventArgs> DeviceFound;

        public ButtplugService()
        {
            mBluetooth = new BluetoothManager();
            mBluetooth.DeviceFound += OnDeviceFound;
            mBluetooth.StartScanning();
        }

        private void OnDeviceFound(object mgr, DeviceFoundEventArgs e)
        {
            DeviceFound?.Invoke(this, e);
        }
    }
}
