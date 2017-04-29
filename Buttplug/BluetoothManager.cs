using System;
using System.Diagnostics;
using Windows.Devices.Bluetooth.Advertisement;
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

        private BluetoothLEAdvertisementWatcher mBleWatcher = null;
        public event EventHandler<DeviceFoundEventArgs> DeviceFound;

        public BluetoothManager()
        {
            mBleWatcher = new BluetoothLEAdvertisementWatcher();
            mBleWatcher.AdvertisementFilter.Advertisement.LocalName = "Launch";
            mBleWatcher.Received += OnAdvertisementReceived;
        }

        public async void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher o,
                                                  BluetoothLEAdvertisementReceivedEventArgs e)
        {
            Option<BluetoothLEDevice> dev = await BluetoothLEDevice.FromBluetoothAddressAsync(e.BluetoothAddress);
            Option<IButtplugDevice> l = null;
            dev.IfSome(async d => l = await FleshlightLaunch.CreateDevice(d));
            l.IfSome(d => DeviceFound?.Invoke(this, new DeviceFoundEventArgs(d)));
        }

        public void StartScanning()
        {
            mBleWatcher.Start();
        }

        public void AddServiceFilter(Guid aServiceFilter)
        {
        }

        

    }
}
