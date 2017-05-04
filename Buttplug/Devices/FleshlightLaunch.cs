using System;
using System.Linq;
using System.Threading.Tasks;
using LanguageExt;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Buttplug.Messages;
using NLog;

namespace Buttplug.Devices
{
    struct FleshlightLaunchBluetoothInfo
    {
        static public readonly Guid SERVICE = new Guid("88f80580-0000-01e6-aace-0002a5d5c51b");
        static public readonly Guid TX_CHAR = new Guid("88f80581-0000-01e6-aace-0002a5d5c51b");
        static public readonly Guid RX_CHAR = new Guid("88f80582-0000-01e6-aace-0002a5d5c51b");
        static public readonly Guid CMD_CHAR = new Guid("88f80583-0000-01e6-aace-0002a5d5c51b");
    }

    class FleshlightLaunchDeviceFactory : ButtplugBluetoothDeviceFactory
    {
        public FleshlightLaunchDeviceFactory()
        {
            // For advertising, the launch only shows up as "Launch", and doesn't advertise any service Uuids.
            NameFilters.Add("Launch");
        }

        public override async Task<Option<ButtplugDevice>> CreateDeviceAsync(BluetoothLEDevice aDevice)
        {
            GattDeviceServicesResult srvResult = await aDevice.GetGattServicesForUuidAsync(FleshlightLaunchBluetoothInfo.SERVICE, BluetoothCacheMode.Cached);
            if (srvResult.Status != GattCommunicationStatus.Success || !srvResult.Services.Any())
            {
                return Option<ButtplugDevice>.None;
            }
            var service = srvResult.Services.First();

            GattCharacteristicsResult chrResult = await service.GetCharacteristicsAsync();
            if (chrResult.Status != GattCommunicationStatus.Success)
            {
                return Option<ButtplugDevice>.None;
            }

            var chrs =
                (from x in chrResult.Characteristics
                 where x.Uuid == FleshlightLaunchBluetoothInfo.TX_CHAR
                 from y in chrResult.Characteristics
                 where y.Uuid == FleshlightLaunchBluetoothInfo.RX_CHAR
                 from z in chrResult.Characteristics
                 where z.Uuid == FleshlightLaunchBluetoothInfo.CMD_CHAR
                 select (x, y, z));

            if (!chrs.Any())
            {
                return Option<ButtplugDevice>.None;
            }

            GattCharacteristic tx = null;
            GattCharacteristic rx = null;
            GattCharacteristic cmd = null;
            (tx, rx, cmd) = chrs.First().ToTuple();
            return Option<ButtplugDevice>.Some(new FleshlightLaunch(aDevice, tx, rx, cmd));
        }
    }

    class FleshlightLaunch : ButtplugBluetoothDevice
    {
        private GattCharacteristic WriteChr;
        private GattCharacteristic ButtonNotifyChr;
        private GattCharacteristic CommandChr;

        public FleshlightLaunch(BluetoothLEDevice aDevice,
                                GattCharacteristic aWriteChr,
                                GattCharacteristic aButtonNotifyChr,
                                GattCharacteristic aCommandChr) :
            base("Fleshlight Launch", aDevice)
        {
            this.BLEDevice = aDevice;
            this.WriteChr = aWriteChr;
            this.ButtonNotifyChr = aButtonNotifyChr;
            this.CommandChr = aCommandChr;
        }

        public override async Task<bool> ParseMessage(IButtplugDeviceMessage msg)
        {
            switch (msg)
            {
                //TODO: Split into Command message and Control message? (Issue #17)
                case FleshlightLaunchRawMessage m:
                    BPLogger.Trace("Sending Fleshlight Launch Command");
                    return true;
            }

            return false;
        }
    }
}
