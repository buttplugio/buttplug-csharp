using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LanguageExt;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using ButtplugMessages;
using Buttplug;

namespace ButtplugDevices
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
 
        async public override Task<Option<IButtplugDevice>> CreateDeviceAsync(BluetoothLEDevice aDevice)
        {
            GattDeviceServicesResult srvResult = await aDevice.GetGattServicesForUuidAsync(FleshlightLaunchBluetoothInfo.SERVICE, BluetoothCacheMode.Cached);
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
                (from x in chrResult.Characteristics
                 where x.Uuid == FleshlightLaunchBluetoothInfo.TX_CHAR
                 from y in chrResult.Characteristics
                 where y.Uuid == FleshlightLaunchBluetoothInfo.RX_CHAR
                 from z in chrResult.Characteristics
                 where z.Uuid == FleshlightLaunchBluetoothInfo.CMD_CHAR
                 select (x, y, z));

            if (!chrs.Any())
            {
                return Option<IButtplugDevice>.None;
            }

            GattCharacteristic tx = null;
            GattCharacteristic rx = null;
            GattCharacteristic cmd = null;
            (tx, rx, cmd) = chrs.First().ToTuple();
            return Option<IButtplugDevice>.Some(new FleshlightLaunch(0, aDevice, tx, rx, cmd));
        }
    }

    class FleshlightLaunch : IButtplugDevice
    {
        public String Name { get; }
        public UInt32 DeviceIndex { get; }
        private BluetoothLEDevice LaunchDevice;
        private GattCharacteristic WriteChr;
        private GattCharacteristic ButtonNotifyChr;
        private GattCharacteristic CommandChr;

        public FleshlightLaunch(UInt32 aDeviceIndex,
                                BluetoothLEDevice aDevice,
                                GattCharacteristic aWriteChr,
                                GattCharacteristic aButtonNotifyChr,
                                GattCharacteristic aCommandChr)
        {
            this.DeviceIndex = aDeviceIndex;
            this.Name = aDevice.Name;
            this.LaunchDevice = aDevice;
            this.WriteChr = aWriteChr;
            this.ButtonNotifyChr = aButtonNotifyChr;
            this.CommandChr = aCommandChr;
        }

        public bool ParseMessage(IButtplugDeviceMessage msg)
        {
            switch (msg)
            {
                case FleshlightLaunchRawMessage m:
                    Console.WriteLine("Got a FleshlightLaunchRawMessage!");
                    break;
            }

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
