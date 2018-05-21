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
            WriteShock = 0,
            WriteVibrate,
            ReadBattery,
        }

        public Guid[] Services { get; } =
        {
            new Guid("00006000-0000-1000-8000-00805f9b34fb"), // Write Service

            // TODO Commenting out battery service until we can handle multiple services.

            // new Guid("00006050-0000-1000-8000-00805f9b34fb"), // Read service (battery)
        };

        public string[] NamePrefixes { get; } = { };

        public string[] Names { get; } =
        {
            "PiPiJing"
        };

        public Dictionary<uint, Guid> Characteristics { get; } = new Dictionary<uint, Guid>()
        {
            // tx1 characteristic
            { (uint)Chrs.WriteShock, new Guid("00006001-0000-1000-8000-00805f9b34fb") }, // Shock

            // tx2 characteristic
            { (uint)Chrs.WriteVibrate, new Guid("00006002-0000-1000-8000-00805f9b34fb") }, // VibeMode

            // rx characteristic
            { (uint)Chrs.ReadBattery,  new Guid("00006051-0000-1000-8000-00805f9b34fb") }, // Read for battery level
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
        private readonly double[] _vibratorSpeed = { 0 };

        public LiBo(IButtplugLogManager aLogManager,
                      IBluetoothDeviceInterface aInterface,
                      IBluetoothDeviceInfo aInfo)
            : base(aLogManager,
                   $"LiBo ({aInterface.Name})",
                   aInterface,
                   aInfo)
        {
            MsgFuncs.Add(typeof(SingleMotorVibrateCmd), new ButtplugDeviceWrapper(HandleSingleMotorVibrateCmd));
            MsgFuncs.Add(typeof(VibrateCmd), new ButtplugDeviceWrapper(HandleVibrateCmd, new MessageAttributes() { FeatureCount = _vibratorCount }));

            // TODO Add a handler for Estim shocking, add a battery handler.
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

            return await HandleVibrateCmd(VibrateCmd.Create(cmdMsg.DeviceIndex, cmdMsg.Id, cmdMsg.Speed, _vibratorCount));
        }

        private async Task<ButtplugMessage> HandleVibrateCmd(ButtplugDeviceMessage aMsg)
        {
            if (!(aMsg is VibrateCmd cmdMsg))
            {
                return BpLogger.LogErrorMsg(aMsg.Id, Error.ErrorClass.ERROR_DEVICE, "Wrong Handler");
            }

            if (cmdMsg.Speeds.Count != _vibratorCount)
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

            // Map a 0 - 100% value to a 0 - 3 value since 0 * x == 0 this will turn off the vibe if
            // speed is 0.00
            int mode = (int)Math.Ceiling(_vibratorSpeed[0] * 3);

            var data = new byte[] { Convert.ToByte(mode) };

            return await Interface.WriteValue(aMsg.Id,
                (uint)LiBoBluetoothInfo.Chrs.WriteVibrate,
                data);
        }
    }
}