using Buttplug.Core;
using Buttplug.Messages;
using Buttplug.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using ButtplugUWPBluetoothManager.Core;

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

        public ButtplugBluetoothDevice CreateDevice(
            IButtplugLogManager aLogManager,
            BluetoothLEDevice aDevice,
            Dictionary<Guid, GattCharacteristic> aCharacteristics)
        {
            return new FleshlightLaunch(aLogManager,
                                        aDevice,
                                        aCharacteristics[Characteristics[0]],
                                        aCharacteristics[Characteristics[1]],
                                        aCharacteristics[Characteristics[2]]);
        }
    }

    internal class FleshlightLaunch : ButtplugBluetoothDevice
    {
        private readonly GattCharacteristic _buttonNotifyChr;
        private readonly GattCharacteristic _commandChr;
        private readonly Stopwatch _stopwatch;
        private readonly ushort _previousSpeed;
        private readonly ushort _previousPosition;
        private ushort _previousKiirooPosition;
        private ushort _limitedSpeed;

        public FleshlightLaunch(IButtplugLogManager aLogManager,
                                BluetoothLEDevice aDevice,
                                GattCharacteristic aWriteChr,
                                GattCharacteristic aButtonNotifyChr,
                                GattCharacteristic aCommandChr) :
            base(aLogManager,
                 "Fleshlight Launch",
                 aDevice,
                 aWriteChr,
                 aButtonNotifyChr)
        {
            BleDevice = aDevice;
            _buttonNotifyChr = _readChr;
            _commandChr = aCommandChr;
            _stopwatch = new Stopwatch();
            _previousSpeed = 0;
            _previousPosition = 0;

            // Setup message function array
            MsgFuncs.Add(typeof(FleshlightLaunchRawCmd), HandleFleshlightLaunchRawCmd);
            MsgFuncs.Add(typeof(KiirooRawCmd), HandleKiirooRawCmd);
        }

        public override async Task<ButtplugMessage> Initialize()
        {
            BpLogger.Trace($"Initializing {Name}");
            var x = await _commandChr.WriteValueAsync(ButtplugBluetoothUtils.WriteByteArray(new byte[] { 0 }));
            if (x != GattCommunicationStatus.Success)
            {
                return BpLogger.LogErrorMsg(0, $"Cannot initialize {Name}!");
            }
            BpLogger.Trace($"{Name} initialized");
            return new Ok(0);
        }

        public async Task<ButtplugMessage> HandleKiirooRawCmd(ButtplugDeviceMessage aMsg)
        {
            var kiirooCmd = aMsg as KiirooRawCmd;
            if (kiirooCmd is null)
            {
                return BpLogger.LogErrorMsg(aMsg.Id, "Wrong Handler");
            }

            var elapsed = _stopwatch.ElapsedMilliseconds;
            _stopwatch.Stop();
            var kiirooPosition = kiirooCmd.Position;
            if (kiirooPosition == _previousKiirooPosition)
            {
                return await HandleFleshlightLaunchRawCmd(new FleshlightLaunchRawCmd(aMsg.DeviceIndex, 0, _previousPosition, aMsg.Id));
            }
            _previousKiirooPosition = kiirooPosition;
            ushort speed = 0;

            // Speed Conversion
            if (elapsed > 2000)
            {
                speed = 50;
            }
            else if (elapsed > 1000)
            {
                speed = 20;
            }
            else
            {
                speed = (ushort)(100 - ((elapsed / 100) + ((elapsed / 100) * .1)));
                if (speed > _previousSpeed)
                {
                    speed = (ushort)(_previousSpeed + ((speed - _previousSpeed) / 6));
                }
                else if (speed <= _previousSpeed)
                {
                    speed = (ushort)(_previousSpeed - (speed / 2));
                }
            }
            if (speed < 20)
            {
                speed = 20;
            }
            _stopwatch.Start();
            // Position Conversion
            if (elapsed <= 150)
            {
                if (_limitedSpeed == 0)
                {
                    _limitedSpeed = speed;
                }
                var position = (ushort)(kiirooPosition > 2 ? 95 : 5);
                return await HandleFleshlightLaunchRawCmd(new FleshlightLaunchRawCmd(aMsg.DeviceIndex, _limitedSpeed, position, aMsg.Id));
            }
            else
            {
                _limitedSpeed = 0;
                var position = (ushort)(kiirooPosition > 2 ? 95 : 5);
                return await HandleFleshlightLaunchRawCmd(new FleshlightLaunchRawCmd(aMsg.DeviceIndex, speed, position, aMsg.Id));
            }
        }

        public async Task<ButtplugMessage> HandleFleshlightLaunchRawCmd(ButtplugDeviceMessage aMsg)
        {
            //TODO: Split into Command message and Control message? (Issue #17)
            var cmdMsg = aMsg as FleshlightLaunchRawCmd;
            if (cmdMsg is null)
            {
                return BpLogger.LogErrorMsg(aMsg.Id, "Wrong Handler");
            }
            return await WriteToDevice(aMsg, ButtplugBluetoothUtils.WriteByteArray(new byte[] { (byte)cmdMsg.Position, (byte)cmdMsg.Speed }));            
        }
    }
}