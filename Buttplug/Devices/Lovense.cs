using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Buttplug.Messages;
using Windows.Storage.Streams;
using LanguageExt;
using NLog;

namespace Buttplug.Devices
{
    struct LovenseBluetoothInfo
    {
        static public readonly Guid SERVICE = new Guid("6e400001-b5a3-f393-e0a9-e50e24dcca9e");
        static public readonly Guid TX_CHAR = new Guid("6e400002-b5a3-f393-e0a9-e50e24dcca9e");
        static public readonly Guid RX_CHAR = new Guid("6e400003-b5a3-f393-e0a9-e50e24dcca9e");
    }

    class LovenseDeviceFactory : ButtplugBluetoothDeviceFactory
    {
        public LovenseDeviceFactory()
        {
            NameFilters.Add("LVS-S001");
            NameFilters.Add("LVS-Z001");
        }

        async public override Task<Option<ButtplugDevice>> CreateDeviceAsync(BluetoothLEDevice aDevice)
        {
            GattDeviceServicesResult srvResult = await aDevice.GetGattServicesForUuidAsync(LovenseBluetoothInfo.SERVICE, BluetoothCacheMode.Cached);
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
                 where x.Uuid == LovenseBluetoothInfo.TX_CHAR
                 from y in chrResult.Characteristics
                 where y.Uuid == LovenseBluetoothInfo.RX_CHAR
                 select (x, y));

            if (!chrs.Any())
            {
                return Option<ButtplugDevice>.None;
            }

            GattCharacteristic tx = null;
            GattCharacteristic rx = null;
            (tx, rx) = chrs.First().ToTuple();
            return Option<ButtplugDevice>.Some(new Lovense(aDevice, tx, rx));
        }
    }

    class Lovense : ButtplugDevice
    {
        private BluetoothLEDevice LovenseDevice;
        private GattCharacteristic WriteChr;
        private GattCharacteristic ReadChr;

        public Lovense(BluetoothLEDevice aDevice,
                       GattCharacteristic aWriteChr,
                       GattCharacteristic aReadChr) :
            base($"Lovense Device ({aDevice.Name})")
        {
            this.LovenseDevice = aDevice;
            this.WriteChr = aWriteChr;
            this.ReadChr = aReadChr;
        }

        public override async Task<bool> ParseMessage(IButtplugDeviceMessage msg)
        {
            switch (msg)
            {
                case SingleMotorVibrateMessage m:
                    BPLogger.Trace("Lovense toy got SingleMotorVibrateMessage");
                    var writer = new DataWriter();
                    writer.WriteString($"Vibrate:{(int)(m.Speed * 20)};");
                    IBuffer buf = writer.DetachBuffer();
                    BPLogger.Trace(buf);
                    await WriteChr.WriteValueAsync(buf);
                    return true;
            }

            return false;
        }
    }
}
