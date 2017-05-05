using Buttplug.Core;
using LanguageExt;
using NLog;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Buttplug
{
    internal class ButtplugBluetoothDeviceFactory
    {
        private readonly Logger _bpLogger;
        private IBluetoothDeviceInfo _deviceInfo;
        public ButtplugBluetoothDeviceFactory(IBluetoothDeviceInfo aInfo)
        {
            _bpLogger = LogManager.GetLogger("Buttplug");
            _bpLogger.Trace($"Creating {this.GetType().Name}");
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
            return !gattCharacteristics.Any() ? Option<ButtplugDevice>.None : Option<ButtplugDevice>.Some(_deviceInfo.CreateDevice(aDevice, gattCharacteristics.ToArray()));
        }
    }
}
