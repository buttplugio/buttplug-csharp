using System;
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
    class FleshlightLaunchBluetoothInfo : IBluetoothDeviceInfo
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
            return new FleshlightLaunch(aDevice, aCharacteristics[0], aCharacteristics[1], aCharacteristics[2]);
        }
    }

    class FleshlightLaunch : ButtplugBluetoothDevice
    {
        private GattCharacteristic WriteChr;
        private GattCharacteristic ButtonNotifyChr;
        private GattCharacteristic CommandChr;

        public FleshlightLaunch(BluetoothLEDevice aDevice,
                                GattCharacteristic aWriteChr,
                                GattCharacteristic aButtonNotifyChr,
                                GattCharacteristic aCommandChr) :
            base("Fleshlight Launch", aDevice)
        {
            this.BLEDevice = aDevice;
            this.WriteChr = aWriteChr;
            this.ButtonNotifyChr = aButtonNotifyChr;
            this.CommandChr = aCommandChr;
        }

        public override async Task<bool> ParseMessage(IButtplugDeviceMessage msg)
        {
            switch (msg)
            {
                //TODO: Split into Command message and Control message? (Issue #17)
                case FleshlightLaunchRawMessage m:
                    BPLogger.Trace("Sending Fleshlight Launch Command");
                    return true;
            }

            return false;
        }
    }
}
