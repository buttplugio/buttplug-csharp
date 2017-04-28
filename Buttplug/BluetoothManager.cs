using System;
using System.Diagnostics;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth;
using LanguageExt;

namespace Buttplug
{
    public class DeviceFoundEventArgs : EventArgs
    {
        public DeviceFoundEventArgs(IButtplugDevice d)
        {
            this.device = d;
        }

        public IButtplugDevice device;
    }

    public class BluetoothManager
    {
        const int BLEWATCHER_STOP_TIMEOUT = 1;          // minute

        private DeviceWatcher mBleDeviceWatcher = null;
        public event EventHandler<DeviceFoundEventArgs> DeviceFound;

        public BluetoothManager()
        {
            string[] reqProps = { "System.Devices.Aep.DeviceAddress", "System.ItemNameDisplay", "System.Devices.Aep.ModelName" };
            mBleDeviceWatcher = DeviceInformation.CreateWatcher(
                "",
                reqProps,
                DeviceInformationKind.AssociationEndpoint);
            mBleDeviceWatcher.Added += DeviceWatcher_Added;
            mBleDeviceWatcher.Updated += DeviceWatcher_Updated;
            mBleDeviceWatcher.Removed += DeviceWatcher_Removed;
            mBleDeviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
        }

        public async void DeviceWatcher_Added(DeviceWatcher watcher, DeviceInformation info)
        {
            //Debug.WriteLine("Added: " + info.Id + " " + info.Name);
            
            if (info.Name == "Launch")
            {
                Option<IButtplugDevice> device = await FleshlightLaunch.CreateDevice(info);
                device.IfSome(x => DeviceFound?.Invoke(this, new DeviceFoundEventArgs(x)));
            }
        }

        public void DeviceWatcher_Updated(DeviceWatcher watcher, DeviceInformationUpdate info)
        {
            //Debug.WriteLine("Updated: " + info.Id);
        }

        public void DeviceWatcher_Removed(DeviceWatcher watcher, DeviceInformationUpdate info)
        {
            //Debug.WriteLine("Removed: " + info.Id);
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
