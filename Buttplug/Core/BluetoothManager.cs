using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth;
using LanguageExt;
using NLog;

namespace Buttplug
{
    class BluetoothManager : DeviceManager
    {
        const int BLEWATCHER_STOP_TIMEOUT = 1;          // minute

        private readonly BluetoothLEAdvertisementWatcher BleWatcher;
        private readonly List<ButtplugBluetoothDeviceFactory> DeviceFactories;

        public BluetoothManager()
        {
            // Introspect the ButtplugDevices namespace for all Factory classes, then create instances of all of them.
            DeviceFactories = new List<ButtplugBluetoothDeviceFactory>();
            AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(t => t.GetTypes())
                .Where(t => t.IsClass && t.Namespace == "Buttplug.Devices" && typeof(ButtplugBluetoothDeviceFactory).IsAssignableFrom(t))
                .ToList()
                .ForEach(c =>
                {
                    BPLogger.Trace($"Loading Bluetooth Device Factory: {c.Name}");
                    DeviceFactories.Add((ButtplugBluetoothDeviceFactory)Activator.CreateInstance(c));
                });


            BleWatcher = new BluetoothLEAdvertisementWatcher {ScanningMode = BluetoothLEScanningMode.Active};
            // We can't filter device advertisements because you can only add one LocalName filter at a time, meaning we 
            // would have to set up multiple watchers for multiple devices. We'll handle our own filtering via the factory
            // classes whenever we receive a device.
            BleWatcher.Received += OnAdvertisementReceived;
        }

        public async void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher o,
                                                  BluetoothLEAdvertisementReceivedEventArgs e)
        {
            BPLogger.Trace($"Got BLE Advertisement for device: {e.Advertisement.LocalName} / {e.BluetoothAddress}");
            var factories = from x in DeviceFactories
                            where x.MayBeDevice(e.Advertisement) == true
                            select x;
            // We should always have either 0 or 1 factories. 
            if (factories.Count() != 1)
            {
                if (factories.Any())
                {
                    BPLogger.Warn($"Found multiple BLE factories for {e.Advertisement.LocalName} {e.BluetoothAddress}:");
                    factories.ToList().ForEach(x => BPLogger.Warn(x.GetType().Name));
                }
                else
                {
                    BPLogger.Trace("No BLE factories found for device.");
                }
                return;
            }
            var factory = factories.First();
            BPLogger.Debug($"Found BLE factory: {factory.GetType().Name}");
            // If we actually have a factory for this device, go ahead and create the device
            Option<BluetoothLEDevice> dev = await BluetoothLEDevice.FromBluetoothAddressAsync(e.BluetoothAddress);
            Option<ButtplugDevice> l = null;
            dev.IfSome(async d => l = await factory.CreateDeviceAsync(d));
            l.IfSome(d => InvokeDeviceAdded(new DeviceAddedEventArgs(d)));
        }

        public override void StartScanning()
        {
            BPLogger.Trace("Starting BLE Scanning");
            BleWatcher.Start();
        }

        public override void StopScanning()
        {
            BPLogger.Trace("Stopping BLE Scanning");
            BleWatcher.Stop();
        }
    }
}
