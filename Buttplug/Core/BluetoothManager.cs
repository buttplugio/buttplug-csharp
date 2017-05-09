using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;

namespace Buttplug.Core
{
    internal class BluetoothManager : DeviceManager
    {
        private const int BLEWATCHER_STOP_TIMEOUT = 1;          // minute

        private readonly BluetoothLEAdvertisementWatcher _bleWatcher;
        private readonly List<ButtplugBluetoothDeviceFactory> _deviceFactories;

        public BluetoothManager()
        {
            // Introspect the ButtplugDevices namespace for all Factory classes, then create instances of all of them.
            _deviceFactories = new List<ButtplugBluetoothDeviceFactory>();
            AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(t => t.GetTypes())
                .Where(t => t.IsClass && t.Namespace == "Buttplug.Devices" && typeof(IBluetoothDeviceInfo).IsAssignableFrom(t))
                .ToList()
                .ForEach(c =>
                {
                    BpLogger.Trace($"Loading Bluetooth Device Factory: {c.Name}");
                    _deviceFactories.Add(new ButtplugBluetoothDeviceFactory((IBluetoothDeviceInfo)Activator.CreateInstance(c)));
                });

            _bleWatcher = new BluetoothLEAdvertisementWatcher { ScanningMode = BluetoothLEScanningMode.Active };
            // We can't filter device advertisements because you can only add one LocalName filter at a time, meaning we
            // would have to set up multiple watchers for multiple devices. We'll handle our own filtering via the factory
            // classes whenever we receive a device.
            _bleWatcher.Received += OnAdvertisementReceived;
        }

        public async void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher o,
                                                  BluetoothLEAdvertisementReceivedEventArgs e)
        {
            BpLogger.Trace($"Got BLE Advertisement for device: {e.Advertisement.LocalName} / {e.BluetoothAddress}");
            var factories = from x in _deviceFactories
                            where x.MayBeDevice(e.Advertisement)
                            select x;
            // We should always have either 0 or 1 factories.
            var buttplugBluetoothDeviceFactories = factories as ButtplugBluetoothDeviceFactory[] ?? factories.ToArray();
            if (buttplugBluetoothDeviceFactories.Count() != 1)
            {
                if (buttplugBluetoothDeviceFactories.Any())
                {
                    BpLogger.Warn($"Found multiple BLE factories for {e.Advertisement.LocalName} {e.BluetoothAddress}:");
                    buttplugBluetoothDeviceFactories.ToList().ForEach(x => BpLogger.Warn(x.GetType().Name));
                }
                else
                {
                    BpLogger.Trace("No BLE factories found for device.");
                }
                return;
            }
            var factory = buttplugBluetoothDeviceFactories.First();
            BpLogger.Debug($"Found BLE factory: {factory.GetType().Name}");
            // If we actually have a factory for this device, go ahead and create the device
            Option<BluetoothLEDevice> dev = await BluetoothLEDevice.FromBluetoothAddressAsync(e.BluetoothAddress);
            Option<ButtplugDevice> l = null;
            dev.IfSome(async d =>
            {
                // If a device is turned on after scanning has started, windows seems to lose the
                // device handle the first couple of times it tries to deal with the advertisement.
                // Just log the error and hope it reconnects on a later retry.
                try
                {
                    l = await factory.CreateDeviceAsync(d);
                }
                catch (Exception ex)
                {
                    BpLogger.Error($"Cannot connect to device {e.Advertisement.LocalName} {e.BluetoothAddress}: {ex.Message}");
                }
            });
            l.IfSome(d => InvokeDeviceAdded(new DeviceAddedEventArgs(d)));
        }

        public override void StartScanning()
        {
            BpLogger.Trace("Starting BLE Scanning");
            _bleWatcher.Start();
        }

        public override void StopScanning()
        {
            BpLogger.Trace("Stopping BLE Scanning");
            _bleWatcher.Stop();
        }
    }
}