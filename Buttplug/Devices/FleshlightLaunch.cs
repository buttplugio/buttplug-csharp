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
    internal class FleshlightLaunchBluetoothInfo : IBluetoothDeviceInfo
    {
        public string[] Names { get; } = { "Launch" };
        public Guid[] Services { get; } = { new Guid("88f80580-0000-01e6-aace-0002a5d5c51b") };

        public Guid[] Characteristics { get; } =
        {
            // tx
            new Guid("88f80581-0000-01e6-aace-0002a5d5c51b"),
            // rx
            new Guid("88f80582-0000-01e6-aace-0002a5d5c51b"),
            // cmd
            new Guid("88f80583-0000-01e6-aace-0002a5d5c51b")
        };

        public ButtplugBluetoothDevice CreateDevice(BluetoothLEDevice aDevice,
                                                    Dictionary<Guid, GattCharacteristic> aCharacteristics)
        {
            return new FleshlightLaunch(aDevice,
                                        aCharacteristics[Characteristics[0]],
                                        aCharacteristics[Characteristics[1]],
                                        aCharacteristics[Characteristics[2]]);
        }
    }

    internal class FleshlightLaunch : ButtplugBluetoothDevice
    {
        private readonly GattCharacteristic _writeChr;
        private readonly GattCharacteristic _buttonNotifyChr;
        private readonly GattCharacteristic _commandChr;
        private bool _isInitialized;

        public FleshlightLaunch(BluetoothLEDevice aDevice,
                                GattCharacteristic aWriteChr,
                                GattCharacteristic aButtonNotifyChr,
                                GattCharacteristic aCommandChr) :
            base("Fleshlight Launch", aDevice)
        {
            BleDevice = aDevice;
            _isInitialized = false;
            _writeChr = aWriteChr;
            _buttonNotifyChr = aButtonNotifyChr;
            _commandChr = aCommandChr;
        }

        private async Task<Option<Error>> Initialize(uint aId)
        {
            _isInitialized = true;
            BpLogger.Trace($"Initializing {Name}");
            var x = await _commandChr.WriteValueAsync(ButtplugUtils.WriteByteArray(new byte[] { 0 }));
            if (x != GattCommunicationStatus.Success)
            {
                return ButtplugUtils.LogAndError(aId, BpLogger, LogLevel.Error, $"Cannot initialize {Name}!");
            }
            BpLogger.Trace($"{Name} initialized");
            return new OptionNone();
        }

        public override async Task<Either<Error, ButtplugMessage>> ParseMessage(ButtplugDeviceMessage msg)
        {
            switch (msg)
            {
                //TODO: Split into Command message and Control message? (Issue #17)
                case Messages.FleshlightLaunchRawCmd cmdMsg:
                    if (!_isInitialized)
                    {
                        var err = await Initialize(msg.Id);

                        if (err.IsSome)
                        {
                            // TODO No seriously extraction shouldn't be this hard. I'm missing something.
                            var m = new Either<Error, ButtplugMessage>();
                            err.IfSome(z => m = z);
                            return m;
                        }
                    }
                    var x = await _writeChr.WriteValueAsync(ButtplugUtils.WriteByteArray(new byte[] { (byte)cmdMsg.Position, (byte)cmdMsg.Speed }));
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