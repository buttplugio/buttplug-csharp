using System;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Buttplug.Messages;
using Windows.Storage.Streams;
using NLog;

namespace Buttplug.Devices
{
    struct LovenseBluetoothInfo
    {
        static public readonly Guid SERVICE = new Guid("6e400001-b5a3-f393-e0a9-e50e24dcca9e");
        static public readonly Guid TX_CHAR = new Guid("6e400002-b5a3-f393-e0a9-e50e24dcca9e");
        static public readonly Guid RX_CHAR = new Guid("6e400003-b5a3-f393-e0a9-e50e24dcca9e");
    }

    class Lovense : IButtplugDevice
    {
        public String Name { get; }
        private BluetoothLEDevice LovenseDevice;
        private GattCharacteristic WriteChr;
        private GattCharacteristic ReadChr;
        private Logger BPLogger;

        public Lovense(BluetoothLEDevice aDevice,
                       GattCharacteristic aWriteChr,
                       GattCharacteristic aReadChr)
        {
            this.Name = aDevice.Name;
            this.LovenseDevice = aDevice;
            this.WriteChr = aWriteChr;
            this.ReadChr = aReadChr;
        }

        public async Task<bool> ParseMessage(IButtplugDeviceMessage msg)
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
