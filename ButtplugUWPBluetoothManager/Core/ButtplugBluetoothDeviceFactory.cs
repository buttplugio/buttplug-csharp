using LanguageExt;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Buttplug.Messages;
using Buttplug.Core;
using Buttplug.Bluetooth;

namespace ButtplugUWPBluetoothManager.Core
{
    internal class ButtplugBluetoothDeviceFactory
    {
        private readonly IButtplugLog _bpLogger;
        private readonly IBluetoothDeviceInfo _deviceInfo;
        private readonly IButtplugLogManager _buttplugLogManager;

        public ButtplugBluetoothDeviceFactory(IButtplugLogManager aLogManager, IBluetoothDeviceInfo aInfo)
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

        public async Task<Option<IButtplugDevice>> CreateDeviceAsync(BluetoothLEDevice aDevice)
        {
            // GetGattServicesForUuidAsync is 15063 only
            var srvResult = await aDevice.GetGattServicesForUuidAsync(_deviceInfo.Services[0], BluetoothCacheMode.Cached);
            if (srvResult.Status != GattCommunicationStatus.Success || !srvResult.Services.Any())
            {
                _bpLogger.Trace("Cannot find service for device");
                return Option<IButtplugDevice>.None;
            }
            var service = srvResult.Services.First();

            var chrResult = await service.GetCharacteristicsAsync();
            if (chrResult.Status != GattCommunicationStatus.Success)
            {
                return Option<IButtplugDevice>.None;
            }

            var chrs = from x in chrResult.Characteristics
                       where _deviceInfo.Characteristics.Contains(x.Uuid)
                       select x;

            var gattCharacteristics = chrs as GattCharacteristic[] ?? chrs.ToArray();
            if (!gattCharacteristics.Any())
            {
                return new OptionNone();
            }
             
            var bleInterface = new UWPBluetoothDeviceInterface(_buttplugLogManager, 
                aDevice, gattCharacteristics);

            var device = _deviceInfo.CreateDevice(_buttplugLogManager, bleInterface);
            if (await device.Initialize() is Ok)
            {
                return Option<IButtplugDevice>.Some(device);
            }
            // If initialization fails, don't actually send the message back. Just return null, we'll have the info in the logs.
            return Option<IButtplugDevice>.None;
        }
    }
}