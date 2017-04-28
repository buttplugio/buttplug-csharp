using System;
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

        async static public Task<Option<IButtplugDevice>> CreateDevice(DeviceInformation d)
        {
            // TODO don't just completely drop errors, return an Either instead of an Option
            // TODO Also clean up if/else blocks
            BluetoothLEDevice bleDevice = await BluetoothLEDevice.FromIdAsync(d.Id);
            if (bleDevice == null)
            {
                return Option<IButtplugDevice>.None;
            }
            GattDeviceServicesResult srvResult = await bleDevice.GetGattServicesAsync();
            if (srvResult.Status != GattCommunicationStatus.Success)
            {
                return Option<IButtplugDevice>.None;
            }
            // TODO why can't I use IEnumerable.Where here?
            Option<GattDeviceService> service = Option<GattDeviceService>.None;
            foreach (GattDeviceService srv in srvResult.Services)
            {
                if (srv.Uuid == SERVICE)
                {
                    service = Option<GattDeviceService>.Some(srv);
                    break;
                }
            }
            if (service.IsNone)
            {
                return Option<IButtplugDevice>.None;
            }

            GattCharacteristicsResult chrResult = null;
            service.IfSome(async x => chrResult = await x.GetCharacteristicsAsync());
            if (chrResult.Status != GattCommunicationStatus.Success)
            {
                return Option<IButtplugDevice>.None;
            }
            GattCharacteristic tx = null;
            GattCharacteristic rx = null;
            GattCharacteristic cmd = null;
            foreach (GattCharacteristic chr in chrResult.Characteristics)
            {
                if (chr.Uuid == RAUNCH_TX_CHAR)
                {
                    tx = chr;
                    continue;
                }
                else if (chr.Uuid == RAUNCH_RX_CHAR)
                {
                    rx = chr;
                    continue;
                }
                else if (chr.Uuid == RAUNCH_CMD_CHAR)
                {
                    cmd = chr;
                    continue;
                }
            }
            if (tx == null || rx == null || cmd == null)
            {
                return Option<IButtplugDevice>.None;
            }
            return Option<IButtplugDevice>.Some(new FleshlightLaunch(bleDevice, tx, rx, cmd));
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
