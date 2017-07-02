using Buttplug.Bluetooth;
using Buttplug.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using JetBrains.Annotations;
using Microsoft.Win32;

namespace ButtplugUWPBluetoothManager.Core
{
    public class UWPBluetoothManager : BluetoothSubtypeManager
    {
        private const int BLEWATCHER_STOP_TIMEOUT = 1;          // minute

        [NotNull]
        private readonly BluetoothLEAdvertisementWatcher _bleWatcher;
        [NotNull]
        private readonly List<ButtplugBluetoothDeviceFactory> _deviceFactories;
        [NotNull]
        private readonly List<ulong> _currentlyConnecting;

        public static bool HasRegistryKeysSet()
        {
            var accessPerm = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\AppID\{415579bd-5399-48ef-8521-775ebcd647af}", "AccessPermission", "");
            if (accessPerm == null || accessPerm.ToString().Length == 0)
            {
                return false;
            }
            // ReSharper disable once PossibleNullReferenceException
            var appId = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\AppID\" + AppDomain.CurrentDomain.FriendlyName, "AppId", "");
            return appId != null && appId.ToString() == "{415579bd-5399-48ef-8521-775ebcd647af}";
        }

        public UWPBluetoothManager(IButtplugLogManager aLogManager) : base(aLogManager)
        {
            BpLogger.Debug("Loading UWP Bluetooth Manager");
            _currentlyConnecting = new List<ulong>();
            // Introspect the ButtplugDevices namespace for all Factory classes, then create instances of all of them.
            _deviceFactories = new List<ButtplugBluetoothDeviceFactory>();
            BuiltinDevices.ForEach(c =>
                {
                    BpLogger.Debug($"Loading Bluetooth Device Factory: {c.GetType().Name}");
                    _deviceFactories.Add(new ButtplugBluetoothDeviceFactory(aLogManager, c));
                });

            _bleWatcher = new BluetoothLEAdvertisementWatcher { ScanningMode = BluetoothLEScanningMode.Active };
            // We can't filter device advertisements because you can only add one LocalName filter at a time, meaning we
            // would have to set up multiple watchers for multiple devices. We'll handle our own filtering via the factory
            // classes whenever we receive a device.
            _bleWatcher.Received += OnAdvertisementReceived;
            _bleWatcher.Stopped += OnWatcherStopped;
        }

        private async void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher o,
                                                  BluetoothLEAdvertisementReceivedEventArgs e)
        {
            //BpLogger.Trace($"Got BLE Advertisement for device: {e.Advertisement.LocalName} / {e.BluetoothAddress}");
            if (_currentlyConnecting.Contains(e.BluetoothAddress))
            {
                //BpLogger.Trace($"Ignoring advertisement for already connecting device: {e.Advertisement.LocalName} / {e.BluetoothAddress}");
                return;
            }
            BpLogger.Trace("BLE device found: " + e.Advertisement.LocalName);
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
                    //BpLogger.Trace("No BLE factories found for device.");
                }
                return;
            }
            _currentlyConnecting.Add(e.BluetoothAddress);
            var factory = buttplugBluetoothDeviceFactories.First();
            BpLogger.Debug($"Found BLE factory: {factory.GetType().Name}");
            // If we actually have a factory for this device, go ahead and create the device
            var fromBluetoothAddressAsync = BluetoothLEDevice.FromBluetoothAddressAsync(e.BluetoothAddress);
            if (fromBluetoothAddressAsync != null)
            {
                var dev = await fromBluetoothAddressAsync;
                // If a device is turned on after scanning has started, windows seems to lose the
                // device handle the first couple of times it tries to deal with the advertisement.
                // Just log the error and hope it reconnects on a later retry.
                try
                {
                    var d = await factory.CreateDeviceAsync(dev);
                    InvokeDeviceAdded(new DeviceAddedEventArgs(d));
                }
                catch (Exception ex)
                {
                    BpLogger.Error(
                        $"Cannot connect to device {e.Advertisement.LocalName} {e.BluetoothAddress}: {ex.Message}");
                    _currentlyConnecting.Remove(e.BluetoothAddress);
                    return;
                }
            }
            _currentlyConnecting.Remove(e.BluetoothAddress);
        }

        private void OnWatcherStopped(BluetoothLEAdvertisementWatcher o,
                                      BluetoothLEAdvertisementWatcherStoppedEventArgs e)
        {

            BpLogger.Trace("Stopped BLE Scanning");
            InvokeScanningFinished();
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

        public override bool IsScanning()
        {
            return _bleWatcher.Status == BluetoothLEAdvertisementWatcherStatus.Started;
        }
    }
}