using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth;
using LanguageExt;

namespace Buttplug
{

    class BluetoothManager : IDeviceManager
    {
        const int BLEWATCHER_STOP_TIMEOUT = 1;          // minute

        private BluetoothLEAdvertisementWatcher BleWatcher = null;
        private List<ButtplugBluetoothDeviceFactory> DeviceFactories;


        public BluetoothManager()
        {
            // Introspect the ButtplugDevices namespace for all Factory classes, then create instances of all of them.
            DeviceFactories = new List<ButtplugBluetoothDeviceFactory>();
            var factoryClasses = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(t => t.GetTypes())
                .Where(t => t.IsClass && t.Namespace == "Buttplug.Devices" && typeof(ButtplugBluetoothDeviceFactory).IsAssignableFrom(t));
            factoryClasses.ToList().ForEach(c => DeviceFactories.Add((ButtplugBluetoothDeviceFactory)Activator.CreateInstance(c)));

            BleWatcher = new BluetoothLEAdvertisementWatcher();
            BleWatcher.ScanningMode = BluetoothLEScanningMode.Active;
            // We can't filter device advertisements because you can only add one LocalName filter at a time, meaning we 
            // would have to set up multiple watchers for multiple devices. We'll handle our own filtering via the factory
            // classes whenever we receive a device.
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
            l.IfSome(d => InvokeDeviceAdded(new DeviceAddedEventArgs(d)));
        }

        public override void StartScanning()
        {
            BleWatcher.Start();
        }

        public override void StopScanning()
        {
            BleWatcher.Stop();
        }
    }
}
