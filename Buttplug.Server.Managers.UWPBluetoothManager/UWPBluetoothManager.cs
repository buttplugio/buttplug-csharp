// <copyright file="UWPBluetoothManager.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            BpLogger.Info("Loading UWP Bluetooth Manager");
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
            var adapterTask = Task.Run(() => BluetoothAdapter.GetDefaultAsync().AsTask());
            adapterTask.Wait();
            var adapter = adapterTask.Result;
            if (adapter == null)
            {
                BpLogger.Warn("No bluetooth adapter available for UWP Bluetooth Manager Connection");
                return;
            }

            if (!adapter.IsLowEnergySupported)
            {
                BpLogger.Warn("Bluetooth adapter available but does not support Bluetooth Low Energy.");
                return;
            }

            BpLogger.Debug("UWP Manager found working Bluetooth LE Adapter");
        }

        private async void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher aObj,
                                                   BluetoothLEAdvertisementReceivedEventArgs aEvent)
        {
            if (aEvent?.Advertisement == null)
            {
                BpLogger.Debug("Null BLE advertisement recieved: skipping");
                return;
            }

            var advertName = aEvent.Advertisement.LocalName ?? string.Empty;
            var advertGUIDs = new List<Guid>();
            advertGUIDs.AddRange(aEvent.Advertisement.ServiceUuids ?? new Guid[] { });
            var btAddr = aEvent.BluetoothAddress;

            // BpLogger.Trace($"Got BLE Advertisement for device: {aEvent.Advertisement.LocalName} / {aEvent.BluetoothAddress}");
            if (_currentlyConnecting.Contains(btAddr))
            {
                // BpLogger.Trace($"Ignoring advertisement for already connecting device: {aEvent.Advertisement.LocalName} / {aEvent.BluetoothAddress}");
                return;
            }

            BpLogger.Trace("BLE device found: " + advertName);
            var factories = from x in _deviceFactories
                            where x.MayBeDevice(advertName, advertGUIDs)
                            select x;

            // We should always have either 0 or 1 factories.
            var buttplugBluetoothDeviceFactories = factories as UWPBluetoothDeviceFactory[] ?? factories.ToArray();
            if (buttplugBluetoothDeviceFactories.Length != 1)
            {
                if (buttplugBluetoothDeviceFactories.Any())
                {
                    BpLogger.Warn($"Found multiple BLE factories for {advertName} {btAddr}:");
                    buttplugBluetoothDeviceFactories.ToList().ForEach(x => BpLogger.Warn(x.GetType().Name));
                }
                else
                {
                    // BpLogger.Trace("No BLE factories found for device.");
                }

                return;
            }

            _currentlyConnecting.Add(btAddr);
            var factory = buttplugBluetoothDeviceFactories.First();
            BpLogger.Debug($"Found BLE factory: {factory.GetType().Name}");

            // If we actually have a factory for this device, go ahead and create the device
            var fromBluetoothAddressAsync = BluetoothLEDevice.FromBluetoothAddressAsync(btAddr);
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
                        $"Cannot connect to device {advertName} {btAddr}: {ex.Message}");
                    _currentlyConnecting.Remove(btAddr);
                    return;
                }
            }

            _currentlyConnecting.Remove(btAddr);
        }

        private void OnWatcherStopped(BluetoothLEAdvertisementWatcher aObj,
                                      BluetoothLEAdvertisementWatcherStoppedEventArgs aEvent)
        {
            BpLogger.Info("Stopped BLE Scanning");
            InvokeScanningFinished();
        }

        public override void StartScanning()
        {
            BpLogger.Info("Starting BLE Scanning");
            _bleWatcher.Start();
        }

        public override void StopScanning()
        {
            BpLogger.Info("Stopping BLE Scanning");
            _bleWatcher.Stop();
        }

        public override bool IsScanning()
        {
            return _bleWatcher.Status == BluetoothLEAdvertisementWatcherStatus.Started;
        }
    }
}