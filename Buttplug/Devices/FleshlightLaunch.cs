using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LanguageExt;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
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

        public ButtplugBluetoothDevice CreateDevice(BluetoothLEDevice aDevice, GattCharacteristic[] aCharacteristics)
        {
            var d = aCharacteristics.ToDictionary(x => x.Uuid, x => x);
            return new FleshlightLaunch(aDevice, d[Characteristics[0]], d[Characteristics[1]], d[Characteristics[2]]);
        }
    }

    internal class FleshlightLaunch : ButtplugBluetoothDevice
    {
        private GattCharacteristic _writeChr;
        private GattCharacteristic _buttonNotifyChr;
        private GattCharacteristic _commandChr;

        public FleshlightLaunch(BluetoothLEDevice aDevice,
                                GattCharacteristic aWriteChr,
                                GattCharacteristic aButtonNotifyChr,
                                GattCharacteristic aCommandChr) :
            base("Fleshlight Launch", aDevice)
        {
            BleDevice = aDevice;
            _writeChr = aWriteChr;
            _buttonNotifyChr = aButtonNotifyChr;
            _commandChr = aCommandChr;
        }

        private void Initialize()
        {
            
        }

#pragma warning disable 1998
        public override async Task<bool> ParseMessage(IButtplugDeviceMessage msg)
#pragma warning restore 1998
        {
            switch (msg)
            {
                //TODO: Split into Command message and Control message? (Issue #17)
                case FleshlightLaunchRawMessage m:
                    BpLogger.Trace("Sending Fleshlight Launch Command");
                    return true;
            }

            return false;
        }
    }
}
