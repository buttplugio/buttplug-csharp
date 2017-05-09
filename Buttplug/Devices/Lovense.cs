using Buttplug.Core;
using Buttplug.Messages;
using LanguageExt;
using NLog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Buttplug.Devices
{
    internal class LovenseBluetoothInfo : IBluetoothDeviceInfo
    {
        public Guid[] Services { get; } = { new Guid("6e400001-b5a3-f393-e0a9-e50e24dcca9e") };
        public string[] Names { get; } = { "LVS-S001", "LVS-Z001" };

        public Guid[] Characteristics { get; } =
        {
            // tx characteristic
            new Guid("6e400002-b5a3-f393-e0a9-e50e24dcca9e"),
            // rx characteristic
            new Guid("6e400003-b5a3-f393-e0a9-e50e24dcca9e")
        };

        public ButtplugBluetoothDevice CreateDevice(BluetoothLEDevice aDevice,
                                                    Dictionary<Guid, GattCharacteristic> aCharacteristics)
        {
            return new Lovense(aDevice,
                               aCharacteristics[Characteristics[0]],
                               aCharacteristics[Characteristics[1]]);
        }
    }

    internal class Lovense : ButtplugBluetoothDevice
    {
        private readonly GattCharacteristic _writeChr;
        private GattCharacteristic _readChr;

        public Lovense(BluetoothLEDevice aDevice,
                       GattCharacteristic aWriteChr,
                       GattCharacteristic aReadChr) :
            base($"Lovense Device ({aDevice.Name})", aDevice)
        {
            _writeChr = aWriteChr;
            _readChr = aReadChr;
        }

        public override async Task<Either<Error, ButtplugMessage>> ParseMessage(ButtplugDeviceMessage aMsg)
        {
            switch (aMsg)
            {
                case Messages.SingleMotorVibrateCmd m:
                    BpLogger.Trace("Lovense toy got SingleMotorVibrateMessage");
                    var buf = ButtplugUtils.WriteString($"Vibrate:{(int)(m.Speed * 20)};");
                    await _writeChr.WriteValueAsync(buf);
                    return new Ok(aMsg.Id);
            }

            return ButtplugUtils.LogAndError(aMsg.Id, BpLogger, LogLevel.Error, $"{Name} cannot handle message of type {aMsg.GetType().Name}");
        }
    }
}