using System;
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

        public bool MayBeDevice(BluetoothLEAdvertisement aAdvertisement)
        {
            if (_deviceInfo.Names.Any() && !_deviceInfo.Names.Contains(aAdvertisement.LocalName))
            {
                return false;
            }

            return !aAdvertisement.ServiceUuids.Any() || _deviceInfo.Services.Union(aAdvertisement.ServiceUuids).Any();
        }

        // TODO Have this throw exceptions instead of return null. Once we've made it this far, if we don't find what we're expecting, that's weird.
        [ItemCanBeNull]
        public async Task<IButtplugDevice> CreateDeviceAsync([NotNull] BluetoothLEDevice aDevice)
        {
            // GetGattServicesForUuidAsync is 15063 only
            var services = await aDevice.GetGattServicesAsync(BluetoothCacheMode.Cached);
            foreach (var s in services.Services)
            {
                _bpLogger.Trace("Found service UUID: " + s.Uuid);
            }

            var srvResult = await aDevice.GetGattServicesForUuidAsync(_deviceInfo.Services[0], BluetoothCacheMode.Cached);
            if (srvResult.Status != GattCommunicationStatus.Success || !srvResult.Services.Any())
            {
                _bpLogger.Trace("Cannot find service for device");
                return null;
            }

            var service = srvResult.Services.First();

            var chrResult = await service.GetCharacteristicsAsync();
            if (chrResult.Status != GattCommunicationStatus.Success)
            {
                return null;
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
