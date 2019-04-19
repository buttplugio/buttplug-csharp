using System;
using System.Collections.Generic;
using Buttplug.Core.Logging;
using Buttplug.Devices.Configuration;
using JetBrains.Annotations;
using Plugin.BluetoothLE;

namespace Buttplug.Server.Managers.XamarinBluetoothManager
{
    class XamarinBluetoothManager : DeviceSubtypeManager
    {
        private IDisposable _scanner;
        [NotNull]
        private readonly List<Guid> _seenAddresses = new List<Guid>();

        public XamarinBluetoothManager([NotNull] IButtplugLogManager aLogManager) : base(aLogManager)
        {
        }

        public override void StartScanning()
        {
            if (_scanner != null)
            {
                return;
            }

            _scanner = CrossBleAdapter.Current.Scan().Subscribe(scanResult =>
            {
                var advertName = scanResult.AdvertisementData.LocalName ?? string.Empty;
                var advertGUIDs = new List<Guid>();
                advertGUIDs.AddRange(scanResult.AdvertisementData.ServiceUuids ?? new Guid[] { });
                var btAddr = scanResult.Device.Uuid;

                BpLogger.Trace($"Got BLE Advertisement for device: {scanResult.AdvertisementData.LocalName} / {scanResult.Device.Uuid}");
                if (_seenAddresses.Contains(btAddr))
                {
                    // BpLogger.Trace($"Ignoring advertisement for already connecting device:
                    // {aEvent.Advertisement.LocalName} / {aEvent.BluetoothAddress}");
                    return;
                }

                BpLogger.Trace("BLE device found: " + advertName);

                // We always need a name to match against.
                if (advertName == string.Empty)
                {
                    return;
                }

                // todo Add advertGUIDs back in. Not sure that ever really gets used though.
                var deviceCriteria = new BluetoothLEProtocolConfiguration(advertName);

                var deviceFactory = DeviceConfigurationManager.Manager.Find(deviceCriteria);

                // If we don't have a protocol to match the device, we can't do anything with it.
                if (deviceFactory == null || !(deviceFactory.Config is BluetoothLEProtocolConfiguration bleConfig))
                {
                    BpLogger.Debug($"No usable device factory available for {advertName}.");
                    // If we've got an actual name this time around, and we don't have any factories
                    // available that match the info we have, add to our seen list so we won't keep
                    // rechecking. If a device does have a factory, but doesn't connect, we still want to
                    // try again.
                    _seenAddresses.Add(btAddr);
                    return;
                }

                BpLogger.Debug($"Found factory for {advertName}");
            });
        }

        public override void StopScanning()
        {
            _scanner.Dispose();
        }

        public override bool IsScanning()
        {
            return _scanner != null;
        }
    }
}
