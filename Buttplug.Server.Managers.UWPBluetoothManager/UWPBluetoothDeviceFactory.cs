using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;
using Buttplug.Server.Bluetooth;
using JetBrains.Annotations;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
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
            if (_deviceInfo.Names.Any() && !_deviceInfo.Names.Contains(advertName))
            {
                return false;
            }

            if (_deviceInfo.Names.Any() && !advertGUIDs.Any())
            {
                _bpLogger.Debug("Found " + advertName + " for " + _deviceInfo.GetType());
                return true;
            }

            _bpLogger.Debug("Found " + advertName + " for " + _deviceInfo.GetType() + " with services " + advertGUIDs);
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
            foreach (var s in services.Services)
            {
                _bpLogger.Debug("Found service UUID: " + s.Uuid + " (" + aDevice.Name + ")");
            }

            var srvResult = await aDevice.GetGattServicesForUuidAsync(_deviceInfo.Services[0], BluetoothCacheMode.Cached);
            if (srvResult.Status != GattCommunicationStatus.Success || !srvResult.Services.Any())
            {
                _bpLogger.Debug("Cannot find service for device (" + aDevice.Name + ")");
                return null;
            }

            var service = srvResult.Services.First();

            var chrResult = await service.GetCharacteristicsAsync();
            if (chrResult.Status != GattCommunicationStatus.Success)
            {
                return null;
            }

            foreach (var s in chrResult.Characteristics)
            {
                _bpLogger.Trace("Found characteristics UUID: " + s.Uuid + " (" + aDevice.Name + ")");
            }

            var chrs = from x in chrResult.Characteristics
                       where _deviceInfo.Characteristics.Contains(x.Uuid)
                       select x;

            var gattCharacteristics = chrs as GattCharacteristic[] ?? chrs.ToArray();
            if (!gattCharacteristics.Any())
            {
                return null;
            }

            // TODO This assumes we're always planning on having the UUIDs sorted in the Info classes, which is probably not true.
            var bleInterface = new UWPBluetoothDeviceInterface(_buttplugLogManager,
                aDevice, gattCharacteristics.OrderBy((aChr) => aChr.Uuid).ToArray());

            var device = _deviceInfo.CreateDevice(_buttplugLogManager, bleInterface);
            if (await device.Initialize() is Ok)
            {
                return device;
            }

            // If initialization fails, don't actually send the message back. Just return null, we'll have the info in the logs.
            return null;
        }
    }
}