using Buttplug.Core;
using System;
using System.Collections.Generic;
using LanguageExt;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Buttplug.Messages;
using LogLevel = NLog.LogLevel;

namespace Buttplug.Devices
{
    internal class KiirooBluetoothInfo : IBluetoothDeviceInfo
    {
        public string[] Names { get; } = { "ONYX", "PEARL" };
        public Guid[] Services { get; } = { new Guid("49535343-fe7d-4ae5-8fa9-9fafd205e455") };

        public Guid[] Characteristics { get; } =
        {
            // tx
            new Guid("49535343-8841-43f4-a8d4-ecbe34729bb3"),
            // rx
            new Guid("49535343-1e4d-4bd9-ba61-23c647249616")
        };

        public ButtplugBluetoothDevice CreateDevice(BluetoothLEDevice aDevice,
            Dictionary<Guid, GattCharacteristic> aCharacteristics)
        {
            return new Kiiroo(aDevice,
                aCharacteristics[Characteristics[0]],
                aCharacteristics[Characteristics[1]]);
        }
    }

    internal class Kiiroo : ButtplugBluetoothDevice
    {
        private readonly GattCharacteristic _writeChr;
        private readonly GattCharacteristic _readChr;

        public Kiiroo(BluetoothLEDevice aDevice,
            GattCharacteristic aWriteChr,
            GattCharacteristic aReadChr) :
            base($"Kiiroo {aDevice.Name}", aDevice)
        {
            _writeChr = aWriteChr;
            _readChr = aReadChr;
        }

        public override async Task<Either<Error, ButtplugMessage>> ParseMessage(ButtplugDeviceMessage msg)
        {
            switch (msg)
            {
                case KiirooRawCmd cmdMsg:
                    var x = await _writeChr.WriteValueAsync(ButtplugUtils.WriteString($"{cmdMsg.Position},\n"));
                    if (x == GattCommunicationStatus.Success)
                    {
                        return new Ok(cmdMsg.Id);
                    }
                    return ButtplugUtils.LogAndError(cmdMsg.Id, BpLogger, LogLevel.Error, $"Cannot send data to {Name}");
            }

            return ButtplugUtils.LogAndError(msg.Id, BpLogger, LogLevel.Error, $"{Name} cannot handle message of type {msg.GetType().Name}");

        }
    }
}