using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;

namespace Buttplug.Server.Bluetooth.Devices
{
    internal class LiBoBluetoothInfo : IBluetoothDeviceInfo
    {
        public enum Chrs : uint
        {
            Tx1 = 0,
            Tx2,
            Rx,
        }

        public Guid[] Services { get; } =
        {
            new Guid("00006000-0000-1000-8000-00805f9b34fb"), // Write Service
            new Guid("00006050-0000-1000-8000-00805f9b34fb"), // Read service (battery)

            // I'm pretty sure that the 2nd one here is ignored due to a call to Services.First() in the bluetooth manager
        };

        public string[] Names { get; } =
        {
            "PiPiJing"
        };

        public Guid[] Characteristics { get; } =
        {
            // tx1 characteristic
            new Guid("00006001-0000-1000-8000-00805f9b34fb"), // Shock

            // tx2 characteristic
            new Guid("00006002-0000-1000-8000-00805f9b34fb"), // VibeMode

            // rx characteristic
            new Guid("00006051-0000-1000-8000-00805f9b34fb"), // Read for battery level
        };

        public IButtplugDevice CreateDevice(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
        {
            return new LiBo(aLogManager, aInterface, this);
        }
    }

    internal class LiBo : ButtplugBluetoothDevice
    {
        private readonly uint _vibratorCount = 1;
        private readonly uint _shockerCount = 1;
        private readonly double[] _vibratorSpeed = { 0 };
        private readonly double[] _shockerIntensity = { 0 };

        public LiBo(IButtplugLogManager aLogManager,
                      IBluetoothDeviceInterface aInterface,
                      IBluetoothDeviceInfo aInfo)
            : base(aLogManager,
                   $"LiBo Device ({aInterface.Name})",
                   aInterface,
                   aInfo)
        {
            MsgFuncs.Add(typeof(SingleMotorVibrateCmd), new ButtplugDeviceWrapper(HandleSingleMotorVibrateCmd));
            MsgFuncs.Add(typeof(VibrateCmd), new ButtplugDeviceWrapper(HandleVibrateCmd, new MessageAttributes() { FeatureCount = _vibratorCount }));
            // Here goes a handler for Estim shocking
            MsgFuncs.Add(typeof(StopDeviceCmd), new ButtplugDeviceWrapper(HandleStopDeviceCmd));
        }

        private async Task<ButtplugMessage> HandleStopDeviceCmd(ButtplugDeviceMessage aMsg)
        {
            return await HandleSingleMotorVibrateCmd(new SingleMotorVibrateCmd(aMsg.DeviceIndex, 0, aMsg.Id));
        }

        private async Task<ButtplugMessage> HandleSingleMotorVibrateCmd(ButtplugDeviceMessage aMsg)
        {
            if (!(aMsg is SingleMotorVibrateCmd cmdMsg))
            {
                return BpLogger.LogErrorMsg(aMsg.Id, Error.ErrorClass.ERROR_DEVICE, "Wrong Handler");
            }

            var subCmds = new List<VibrateCmd.VibrateSubcommand>();
            for (var i = 0u; i < _vibratorCount; i++)
            {
                subCmds.Add(new VibrateCmd.VibrateSubcommand(i, cmdMsg.Speed));
            }

            return await HandleVibrateCmd(new VibrateCmd(cmdMsg.DeviceIndex, subCmds, cmdMsg.Id));
        }

        private async Task<ButtplugMessage> HandleVibrateCmd(ButtplugDeviceMessage aMsg)
        {
            if (!(aMsg is VibrateCmd cmdMsg))
            {
                return BpLogger.LogErrorMsg(aMsg.Id, Error.ErrorClass.ERROR_DEVICE, "Wrong Handler");
            }

            if (cmdMsg.Speeds.Count < 1 || cmdMsg.Speeds.Count > _vibratorCount)
            {
                return new Error(
                    $"VibrateCmd requires between 1 and {_vibratorCount} vectors for this device.",
                    Error.ErrorClass.ERROR_DEVICE,
                    cmdMsg.Id);
            }

            var changed = false;
            foreach (var v in cmdMsg.Speeds)
            {
                if (v.Index >= _vibratorCount)
                {
                    return new Error(
                        $"Index {v.Index} is out of bounds for VibrateCmd for this device.",
                        Error.ErrorClass.ERROR_DEVICE,
                        cmdMsg.Id);
                }

                if (!(Math.Abs(v.Speed - _vibratorSpeed[v.Index]) > 0.001))
                {
                    continue;
                }

                changed = true;
                _vibratorSpeed[v.Index] = v.Speed;
            }

            if (!changed)
            {
                return new Ok(cmdMsg.Id);
            }

            int mode = (int)Math.Ceiling(_vibratorSpeed[0] * 3); // Map a 0 - 100% value to a 0 - 3 value since 0 * x == 0 this will turn off the vibe if speed is 0.00

            var data = new byte[] { Convert.ToByte(mode) };

            return await Interface.WriteValue(aMsg.Id,
                Info.Characteristics[(uint)LiBoBluetoothInfo.Chrs.Tx2],
                data);
        }

        private int getBatteryLevel()
        {
            throw new NotImplementedException("While most of this method is done it still needs to be adjusted/completed - see the comments");
            int batteryLevel = Convert.ToInt32(0x64 /* 100 in hex */);  // Read from Info.Services[1] + Info.Characteristics[uint)LiBoBluetoothInfo.Chrs.Rx] here
            return batteryLevel;                                        // As far as I can tell we currently have no way of reading values
        }

        private async Task<ButtplugMessage> HandleShockCmd(ButtplugDeviceMessage aMsg)
        {
            throw new NotImplementedException("While most of this method is done it still needs to be adjusted/completed - see the comments");
            if (!(aMsg is VibrateCmd cmdMsg)) // ShockCmd once it gets implemented
            {
                return BpLogger.LogErrorMsg(aMsg.Id, Error.ErrorClass.ERROR_DEVICE, "Wrong Handler");
            }

            if (cmdMsg.Speeds.Count < 1 || cmdMsg.Speeds.Count > _shockerCount)
            {
                return new Error(
                    $"ShockCmd requires between 1 and {_shockerCount} vectors for this device.", // Fix cmd name once settled
                    Error.ErrorClass.ERROR_DEVICE,
                    cmdMsg.Id);
            }

            var changed = false;
            foreach (var v in cmdMsg.Speeds)
            {
                if (v.Index >= _shockerCount)
                {
                    return new Error(
                        $"Index {v.Index} is out of bounds for VibrateCmd for this device.", // Fix cmd name once settled
                        Error.ErrorClass.ERROR_DEVICE,
                        cmdMsg.Id);
                }

                if (!(Math.Abs(v.Speed - _shockerIntensity[v.Index]) > 0.001))
                {
                    continue;
                }

                changed = true;
                _shockerIntensity[v.Index] = v.Speed;
            }

            if (!changed)
            {
                return new Ok(cmdMsg.Id);
            }

            int speed = 0;

            // If intensity is set to 0 skip this with 0x00 and turn the shocker off
            if (_shockerIntensity[0] > 0)
            {
                speed = (int)Math.Floor(_shockerIntensity[0] * 7) << 4; // Map a 0 - 100% value to a 0 - 6 value shift 4 to have 0xY0 where Y is speed
                speed++; // add 1 to speed to get 0xY1 where Y is speed and 1 is mode 1 (constant shock)
            }

            var data = new byte[] { Convert.ToByte(speed) };

            return await Interface.WriteValue(aMsg.Id,
                Info.Characteristics[(uint)LiBoBluetoothInfo.Chrs.Tx1],
                data);
        }
    }
}
