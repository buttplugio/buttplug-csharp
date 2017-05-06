using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LanguageExt;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;
using Buttplug.Core;
using Buttplug.Messages;
using NLog;

namespace Buttplug.Devices
{
    internal class FleshlightLaunchBluetoothInfo : IBluetoothDeviceInfo
    {
        public string[] Names { get; } = {"Launch"};
        public Guid[] Services { get; } = {new Guid("88f80580-0000-01e6-aace-0002a5d5c51b")};

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

        private async Task Initialize()
        {
            _isInitialized = true;
            BpLogger.Debug("Initializing Fleshlight Launch");
            var x = await _commandChr.WriteValueAsync(ButtplugUtils.WriteByteArray(new byte[] {0}));
            if (x != GattCommunicationStatus.Success)
            {
                BpLogger.Error("Cannot initialize fleshlight launch device!");
            }
            BpLogger.Debug("Fleshlight Launch Initialized");
        }

#pragma warning disable 1998
        public override async Task<bool> ParseMessage(IButtplugDeviceMessage msg)
#pragma warning restore 1998
        {
            if (!_isInitialized)
            {
                await Initialize();
            }
            switch (msg)
            {
                //TODO: Split into Command message and Control message? (Issue #17)
                case FleshlightLaunchRawMessage m:
                    var x = await _writeChr.WriteValueAsync(ButtplugUtils.WriteByteArray(new byte[] {(byte)m.Position, (byte)m.Speed}));
                    if (x != GattCommunicationStatus.Success)
                    {
                        BpLogger.Error("Cannot send data to fleshlight launch device!");
                    }
                    return true;
            }

            return false;
        }
    }
}
