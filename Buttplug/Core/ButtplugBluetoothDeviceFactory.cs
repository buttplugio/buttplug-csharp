using LanguageExt;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Buttplug.Logging;
using Buttplug.Messages;

namespace Buttplug.Core
{
    internal class ButtplugBluetoothDeviceFactory
    {
        private readonly ILog _bpLogger;
        private readonly IBluetoothDeviceInfo _deviceInfo;

        public ButtplugBluetoothDeviceFactory(IBluetoothDeviceInfo aInfo)
        {
            _bpLogger = LogProvider.GetCurrentClassLogger();
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

        public async Task<Option<ButtplugDevice>> CreateDeviceAsync(BluetoothLEDevice aDevice)
        {
            // GetGattServicesForUuidAsync is 15063 only
            var srvResult = await aDevice.GetGattServicesForUuidAsync(_deviceInfo.Services[0], BluetoothCacheMode.Cached);
            if (srvResult.Status != GattCommunicationStatus.Success || !srvResult.Services.Any())
            {
                _bpLogger.Trace("Cannot find service for device");
                return Option<ButtplugDevice>.None;
            }
            var service = srvResult.Services.First();

            var chrResult = await service.GetCharacteristicsAsync();
            if (chrResult.Status != GattCommunicationStatus.Success)
            {
                return Option<ButtplugDevice>.None;
            }

            var chrs = from x in chrResult.Characteristics
                       where _deviceInfo.Characteristics.Contains(x.Uuid)
                       select x;

            var gattCharacteristics = chrs as GattCharacteristic[] ?? chrs.ToArray();
            if (!gattCharacteristics.Any())
            {
                return new OptionNone();
            }
            var device = _deviceInfo.CreateDevice(aDevice, gattCharacteristics.ToDictionary(x => x.Uuid, x => x));
            if (await device.Initialize() is Ok)
            {
                return Option<ButtplugDevice>.Some(device);
            }
            // If initialization fails, don't actually send the message back. Just return null, we'll have the info in the logs.
            return Option<ButtplugDevice>.None;
        }
    }
}