using System;
using System.Linq;
using System.Threading.Tasks;
using LanguageExt;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Buttplug
{
    class FleshlightLaunch : IButtplugDevice
    {
        static public readonly Guid SERVICE = new Guid("88f80580-0000-01e6-aace-0002a5d5c51b");
        static readonly Guid RAUNCH_TX_CHAR = new Guid("88f80581-0000-01e6-aace-0002a5d5c51b");
        static readonly Guid RAUNCH_RX_CHAR = new Guid("88f80582-0000-01e6-aace-0002a5d5c51b");
        static readonly Guid RAUNCH_CMD_CHAR = new Guid("88f80583-0000-01e6-aace-0002a5d5c51b");
        public String Name { get; }
        private BluetoothLEDevice LaunchDevice;
        private GattCharacteristic WriteChr;
        private GattCharacteristic ButtonNotifyChr;
        private GattCharacteristic CommandChr;

        public FleshlightLaunch(BluetoothLEDevice device,
                                GattCharacteristic WriteChr,
                                GattCharacteristic ButtonNotifyChr,
                                GattCharacteristic CommandChr)
        {
            this.Name = device.Name;
            this.LaunchDevice = device;
            this.WriteChr = WriteChr;
            this.ButtonNotifyChr = ButtonNotifyChr;
            this.CommandChr = CommandChr;
        }

        async static public Task<Option<IButtplugDevice>> CreateDevice(BluetoothLEDevice d)
        {
            // TODO don't just completely drop errors, return an Either instead of an Option
            GattDeviceServicesResult srvResult = await d.GetGattServicesForUuidAsync(SERVICE, BluetoothCacheMode.Cached);
            if (srvResult.Status != GattCommunicationStatus.Success || !srvResult.Services.Any())
            {
                return Option<IButtplugDevice>.None;
            }
            var service = srvResult.Services.First();

            GattCharacteristicsResult chrResult = await service.GetCharacteristicsAsync();
            if (chrResult.Status != GattCommunicationStatus.Success)
            {
                return Option<IButtplugDevice>.None;
            }

            var chrs =
                (from x in chrResult.Characteristics where x.Uuid == RAUNCH_TX_CHAR
                 from y in chrResult.Characteristics where y.Uuid == RAUNCH_RX_CHAR
                 from z in chrResult.Characteristics where z.Uuid == RAUNCH_CMD_CHAR
                 select (x, y, z));

            if (!chrs.Any())
            {
                return Option<IButtplugDevice>.None;
            }

            GattCharacteristic tx = null;
            GattCharacteristic rx = null;
            GattCharacteristic cmd = null;
            (tx, rx, cmd) = chrs.First().ToTuple();
            return Option<IButtplugDevice>.Some(new FleshlightLaunch(d, tx, rx, cmd));
        }

        public bool ParseMessage(ButtplugMessage msg)
        {
            return false;
        }

        public bool Connect()
        {
            return false;
        }

        public bool Disconnect()
        {
            return false;
        }
    }
}
