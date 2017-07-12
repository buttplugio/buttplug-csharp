using System;
using System.Collections.Generic;
using System.Linq;
using Buttplug.Core;
using Buttplug.Server.Bluetooth;
using JetBrains.Annotations;
using Microsoft.Win32;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;

namespace Buttplug.Server.Managers.UWPBluetoothManager
{
    public class UWPBluetoothManager : BluetoothSubtypeManager
    {
        [NotNull]
        private readonly BluetoothLEAdvertisementWatcher _bleWatcher;
        [NotNull]
        private readonly List<UWPBluetoothDeviceFactory> _deviceFactories;
        [NotNull]
        private readonly List<ulong> _currentlyConnecting;

        public static bool HasRegistryKeysSet()
        {
            var accessPerm = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\AppID\{415579bd-5399-48ef-8521-775ebcd647af}", "AccessPermission", string.Empty);
            if (accessPerm == null || accessPerm.ToString().Length == 0)
            {
                return false;
            }

            // ReSharper disable once PossibleNullReferenceException
            var appId = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\AppID\" + AppDomain.CurrentDomain.FriendlyName, "AppId", string.Empty);
            return appId != null && appId.ToString() == "{415579bd-5399-48ef-8521-775ebcd647af}";
        }

        public UWPBluetoothManager(IButtplugLogManager aLogManager)
            : base(aLogManager)
        {
            BpLogger.Debug("Loading UWP Bluetooth Manager");
            _currentlyConnecting = new List<ulong>();

            // Introspect the ButtplugDevices namespace for all Factory classes, then create instances of all of them.
            _deviceFactories = new List<UWPBluetoothDeviceFactory>();
            BuiltinDevices.ForEach(aDeviceFactory =>
                {
                    BpLogger.Debug($"Loading Bluetooth Device Factory: {aDeviceFactory.GetType().Name}");
                    _deviceFactories.Add(new UWPBluetoothDeviceFactory(aLogManager, aDeviceFactory));
                });

            _bleWatcher = new BluetoothLEAdvertisementWatcher { ScanningMode = BluetoothLEScanningMode.Active };

            // We can't filter device advertisements because you can only add one LocalName filter at a time, meaning we
            // would have to set up multiple watchers for multiple devices. We'll handle our own filtering via the factory
            // classes whenever we receive a device.
            _bleWatcher.Received += OnAdvertisementReceived;
            _bleWatcher.Stopped += OnWatcherStopped;
        }

        private async void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher aObj,
                                                  BluetoothLEAdvertisementReceivedEventArgs aEvent)
        {
            // BpLogger.Trace($"Got BLE Advertisement for device: {aEvent.Advertisement.LocalName} / {aEvent.BluetoothAddress}");
            if (_currentlyConnecting.Contains(aEvent.BluetoothAddress))
            {
                // BpLogger.Trace($"Ignoring advertisement for already connecting device: {aEvent.Advertisement.LocalName} / {aEvent.BluetoothAddress}");
                return;
            }

            BpLogger.Trace("BLE device found: " + aEvent.Advertisement.LocalName);
            var factories = from x in _deviceFactories
                            where x.MayBeDevice(aEvent.Advertisement)
                            select x;

            // We should always have either 0 or 1 factories.
            var buttplugBluetoothDeviceFactories = factories as UWPBluetoothDeviceFactory[] ?? factories.ToArray();
            if (buttplugBluetoothDeviceFactories.Length != 1)
            {
                if (buttplugBluetoothDeviceFactories.Any())
                {
                    BpLogger.Warn($"Found multiple BLE factories for {aEvent.Advertisement.LocalName} {aEvent.BluetoothAddress}:");
                    buttplugBluetoothDeviceFactories.ToList().ForEach(x => BpLogger.Warn(x.GetType().Name));
                }
                else
                {
                    // BpLogger.Trace("No BLE factories found for device.");
                }

                return;
            }

            _currentlyConnecting.Add(aEvent.BluetoothAddress);
            var factory = buttplugBluetoothDeviceFactories.First();
            BpLogger.Debug($"Found BLE factory: {factory.GetType().Name}");

            // If we actually have a factory for this device, go ahead and create the device
            var fromBluetoothAddressAsync = BluetoothLEDevice.FromBluetoothAddressAsync(aEvent.BluetoothAddress);
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
                        $"Cannot connect to device {aEvent.Advertisement.LocalName} {aEvent.BluetoothAddress}: {ex.Message}");
                    _currentlyConnecting.Remove(aEvent.BluetoothAddress);
                    return;
                }
            }

            _currentlyConnecting.Remove(aEvent.BluetoothAddress);
        }

        private void OnWatcherStopped(BluetoothLEAdvertisementWatcher aObj,
                                      BluetoothLEAdvertisementWatcherStoppedEventArgs aEvent)
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