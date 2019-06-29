using Buttplug.Core.Logging;
using Buttplug.Devices;
using Buttplug.Devices.Configuration;
using JetBrains.Annotations;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Buttplug.Server.Managers.XamarinManager
{
    public class XamarinBluetoothManager : DeviceSubtypeManager
    {
        private IAdapter _adapter;

        [NotNull]
        private readonly List<string> _seenAddresses = new List<string>();

        public XamarinBluetoothManager(IButtplugLogManager aLogManager)
            : base(aLogManager)
        {
            BpLogger.Info("Loading Xamarin Bluetooth Manager");

            _adapter = CrossBluetoothLE.Current.Adapter;
            if (_adapter == null)
            {
                BpLogger.Warn("No bluetooth adapter available for Xamarin Bluetooth Manager Connection");
                return;
            }
            _adapter.DeviceAdvertised += _adapter_DeviceAdvertised;

            BpLogger.Debug("Xamarin Manager found working Bluetooth LE Adapter");
        }

        private async void _adapter_DeviceAdvertised(object sender, Plugin.BLE.Abstractions.EventArgs.DeviceEventArgs e)
        {
            if (e?.Device == null)
            {
                BpLogger.Debug("Null BLE advertisement received: skipping");
                return;
            }

            var advertName = e.Device.Name;
            var btAddr = e.Device.Id.ToString();

            BpLogger.Trace($"Got BLE Advertisement for device: {advertName} / {btAddr}");
            if (_seenAddresses.Contains(btAddr))
            {
                BpLogger.Trace($"Ignoring advertisement for already connecting device: {btAddr}");
                return;
            }
            _seenAddresses.Add(btAddr);
            BpLogger.Trace("BLE device found: " + advertName);

            // We always need a name to match against.
            if (string.IsNullOrEmpty(advertName))
            {
                return;
            }

            var deviceCriteria = new BluetoothLEProtocolConfiguration(advertName);

            var deviceFactory = DeviceConfigurationManager.Manager.Find(deviceCriteria);

            // If we don't have a protocol to match the device, we can't do anything with it.
            if (deviceFactory == null || !(deviceFactory.Config is BluetoothLEProtocolConfiguration bleConfig))
            {
                BpLogger.Debug($"No usable device factory available for {advertName}.");
                return;
            }

            // If a device is turned on after scanning has started, windows seems to lose the device
            // handle the first couple of times it tries to deal with the advertisement. Just log the
            // error and hope it reconnects on a later retry.
            IButtplugDeviceImpl bleDevice = null;
            IButtplugDevice btDevice = null;
            try
            {
                await _adapter.ConnectToDeviceAsync(e.Device);
                bleDevice = await XamarinBluetoothDeviceInterface.Create(LogManager, bleConfig, e.Device).ConfigureAwait(false);
                btDevice = await deviceFactory.CreateDevice(LogManager, bleDevice).ConfigureAwait(false);
                InvokeDeviceAdded(new DeviceAddedEventArgs(btDevice));
            }
            catch (Exception ex)
            {
                if (btDevice != null)
                {
                    btDevice.Disconnect();
                }
                else
                {
                    bleDevice?.Disconnect();
                }

                BpLogger.Error(
                    $"Cannot connect to device {advertName} {btAddr}: {ex.Message}");
            }
        }

        public override void StartScanning()
        {
            BpLogger.Info("Starting BLE Scanning");
            _seenAddresses.Clear();
            var t = Task.Run(async () =>
            {
                BpLogger.Info("Start BLE Scanning");
                await _adapter.StartScanningForDevicesAsync();
            });
        }

        public override void StopScanning()
        {
            BpLogger.Info("Stopping BLE Scanning");
            var t = Task.Run(async () =>
            {
                await _adapter.StopScanningForDevicesAsync();
                BpLogger.Info("Stopped BLE Scanning");
                InvokeScanningFinished();
            });
        }

        public override bool IsScanning()
        {
            return _adapter.IsScanning;
        }
    }
}
