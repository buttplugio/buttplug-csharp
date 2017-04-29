using System;
using System.Collections.Generic;
using System.Linq;
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

        private BluetoothLEAdvertisementWatcher BleWatcher = null;
        private List<ButtplugBluetoothDeviceFactory> DeviceFactories;
        public event EventHandler<DeviceFoundEventArgs> DeviceFound;

        public BluetoothManager()
        {

            // Introspect the ButtplugDevices namespace for all Factory classes.
            DeviceFactories = new List<ButtplugBluetoothDeviceFactory>();

            var factoryClasses = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(t => t.GetTypes())
                .Where(t => t.IsClass && t.Namespace == "ButtplugDevices" && typeof(ButtplugBluetoothDeviceFactory).IsAssignableFrom(t));

            foreach (var c in factoryClasses)
            {
                DeviceFactories.Add((ButtplugBluetoothDeviceFactory)Activator.CreateInstance(c));
            }

            BleWatcher = new BluetoothLEAdvertisementWatcher();
            BleWatcher.ScanningMode = BluetoothLEScanningMode.Active;
            // We can't filter device advertisements because you can only add one LocalName filter at a time, meaning we would have to set up multiple watchers for multiple devices.
            BleWatcher.Received += OnAdvertisementReceived;
        }

        public async void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher o,
                                                  BluetoothLEAdvertisementReceivedEventArgs e)
        {
            var factories = from x in DeviceFactories
                            where x.MayBeDevice(e.Advertisement) == true
                            select x;
            // We should always have either 0 or 1 factories. 
            // TODO If we have a multiple match, log.
            if (factories.Count() != 1)
            {
                return;
            }
            var factory = factories.First();
            // If we actually have a factory for this device, go ahead and create the device
            Option<BluetoothLEDevice> dev = await BluetoothLEDevice.FromBluetoothAddressAsync(e.BluetoothAddress);
            Option<IButtplugDevice> l = null;
            dev.IfSome(async d => l = await factory.CreateDeviceAsync(d));
            l.IfSome(d => DeviceFound?.Invoke(this, new DeviceFoundEventArgs(d)));
        }

        public void StartScanning()
        {
            BleWatcher.Start();
        }

        public void AddServiceFilter(Guid aServiceFilter)
        {
        }

        

    }
}
