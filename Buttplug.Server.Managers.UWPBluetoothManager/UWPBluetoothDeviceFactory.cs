using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;
using Buttplug.Server.Bluetooth;
using JetBrains.Annotations;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Buttplug.Server.Managers.UWPBluetoothManager
{
    internal class UWPBluetoothDeviceFactory
    {
        [NotNull]
        private readonly IButtplugLog _bpLogger;

        [NotNull]
        private readonly IBluetoothDeviceInfo _deviceInfo;

        [NotNull]
        private readonly IButtplugLogManager _buttplugLogManager;

        public UWPBluetoothDeviceFactory([NotNull] IButtplugLogManager aLogManager, [NotNull] IBluetoothDeviceInfo aInfo)
        {
            _buttplugLogManager = aLogManager;
            _bpLogger = _buttplugLogManager.GetLogger(GetType());
            _bpLogger.Trace($"Creating {GetType().Name}");
            _deviceInfo = aInfo;
        }

        public bool MayBeDevice(string advertName, List<Guid> advertGUIDs)
        {
            if (_deviceInfo.NamePrefixes.Any())
            {
                foreach (var deviceInfoNamePrefix in _deviceInfo.NamePrefixes)
                {
                    if (advertName.IndexOf(deviceInfoNamePrefix) != 0)
                    {
                        continue;
                    }

                    _bpLogger.Debug($"Found {advertName} via NamePrefix {deviceInfoNamePrefix}");
                    return true;
                }
            }

            if (_deviceInfo.Names.Any() && !_deviceInfo.Names.Contains(advertName) || !_deviceInfo.Names.Any())
            {
                _bpLogger.Trace($"Dropping query for {advertName}.");
                return false;
            }

            if (_deviceInfo.Names.Any() && !advertGUIDs.Any())
            {
                _bpLogger.Debug("Found " + advertName + " for " + _deviceInfo.GetType());
                return true;
            }

            _bpLogger.Trace("Found " + advertName + " for " + _deviceInfo.GetType() + " with services " + advertGUIDs);
            foreach (var s in _deviceInfo.Services)
            {
                _bpLogger.Trace("Expecting " + s);
            }

            foreach (var s in advertGUIDs)
            {
                _bpLogger.Trace("Got " + s);
            }

            // Intersect doesn't intersect until the enumerator is called
            var sv = _deviceInfo.Services.Intersect(advertGUIDs);
            foreach (var s in sv)
            {
                _bpLogger.Trace("Matched " + s);
                return true;
            }

            return false;
        }

        // TODO Have this throw exceptions instead of return null. Once we've made it this far, if we don't find what we're expecting, that's weird.
        [ItemCanBeNull]
        public async Task<IButtplugDevice> CreateDeviceAsync([NotNull] BluetoothLEDevice aDevice)
        {
            // GetGattServicesForUuidAsync is 15063 only
            var services = await aDevice.GetGattServicesAsync(BluetoothCacheMode.Cached);
            List<Guid> uuids = new List<Guid>();
            foreach (var s in services.Services)
            {
                _bpLogger.Trace($"Found service UUID: {s.Uuid} ({aDevice.Name})");
                uuids.Add(s.Uuid);
            }

            var srvs = (from x in services.Services
                from y in _deviceInfo.Services
                where x.Uuid == y
                select x).ToArray();

            if (srvs.Length != 1)
            {
                // Somehow we've gotten multiple services back, something we don't currently support.
                _bpLogger.Error($"Found {srvs.Length} services for {aDevice.Name}, which is more/less than 1. Please fix this in the bluetooth definition.");
                return null;
            }

            var service = srvs[0];

            var chrResult = await service.GetCharacteristicsAsync();
            if (chrResult.Status != GattCommunicationStatus.Success)
            {
                _bpLogger.Error($"Cannot connect to service {service.Uuid} of {aDevice.Name}.");
                return null;
            }

            foreach (var s in chrResult.Characteristics)
            {
                _bpLogger.Trace($"Found characteristics UUID: {s.Uuid} ({aDevice.Name})");
            }

            var chrs = chrResult.Characteristics.ToArray();

            // If there aren't any characteristics by this point, something has gone wrong.
            if (!chrs.Any())
            {
                _bpLogger.Error($"Cannot find characteristics for service {service.Uuid} of {aDevice.Name}.");
                return null;
            }

            var bleInterface = new UWPBluetoothDeviceInterface(_buttplugLogManager, _deviceInfo, aDevice, chrs);

            var device = _deviceInfo.CreateDevice(_buttplugLogManager, bleInterface);

            // If initialization fails, don't actually send the message back. Just return null, we'll
            // have the info in the logs.
            return await device.Initialize() is Ok ? device : null;
        }
    }
}
