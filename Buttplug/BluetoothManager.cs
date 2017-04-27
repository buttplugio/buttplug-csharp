using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;

using Windows.Foundation;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using System.Threading;

namespace Buttplug
{
    public class BluetoothManager
    {
        const int BLEWATCHER_STOP_TIMEOUT = 1;          // minute

        private DeviceWatcher mBleDeviceWatcher = null;

        public BluetoothManager()
        {
            string[] reqProps = { "System.Devices.Aep.DeviceAddress", "System.ItemNameDisplay", "System.Devices.Aep.ModelName" };
            mBleDeviceWatcher = DeviceInformation.CreateWatcher(
                BluetoothLEDevice.GetDeviceSelectorFromPairingState(false),
                reqProps,
                DeviceInformationKind.AssociationEndpoint);
            mBleDeviceWatcher.Added += DeviceWatcher_Added;
            mBleDeviceWatcher.Updated += DeviceWatcher_Updated;
            mBleDeviceWatcher.Removed += DeviceWatcher_Removed;
            mBleDeviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
            //mBleDeviceWatcher.Start();
        }

        public void DeviceWatcher_Added(DeviceWatcher watcher, DeviceInformation info)
        {
            Debug.WriteLine("Added: " + info.Id + " " + info.Name);
        }

        public void DeviceWatcher_Updated(DeviceWatcher watcher, DeviceInformationUpdate info)
        {
            Debug.WriteLine("Updated: " + info.Id);
        }

        public void DeviceWatcher_Removed(DeviceWatcher watcher, DeviceInformationUpdate info)
        {
            Debug.WriteLine("Removed: " + info.Id);
        }

        public void DeviceWatcher_EnumerationCompleted(DeviceWatcher watcher, object obj)
        {
            Debug.WriteLine("Done enumerating");
        }

        public void StartScanning()
        {
            mBleDeviceWatcher.Start();
        }

        public void AddServiceFilter(Guid aServiceFilter)
        {
        }

        

    }
}
