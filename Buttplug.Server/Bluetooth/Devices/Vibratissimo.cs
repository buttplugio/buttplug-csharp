using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;

namespace Buttplug.Server.Bluetooth.Devices
{
    internal class VibratissimoBluetoothInfo : IBluetoothDeviceInfo
    {
        public enum Chrs : uint
        {
            TxMode = 0,
            TxSpeed,
            Rx,
        }

        public Guid[] Services { get; } = { new Guid("00001523-1212-efde-1523-785feabcd123") };

        // Device can be renamed, but wildcarding spams our logs and they
        // reuse a common Service UUID, so require it to be the default
        public string[] Names { get; } =
        {
            "Vibratissimo",
        };

        public Dictionary<uint, Guid> Characteristics { get; } = new Dictionary<uint, Guid>()
        {
            { (uint)Chrs.TxMode, new Guid("00001524-1212-efde-1523-785feabcd123") },
            { (uint)Chrs.TxSpeed, new Guid("00001526-1212-efde-1523-785feabcd123") },
            { (uint)Chrs.Rx, new Guid("00001527-1212-efde-1523-785feabcd123") },
        };

        public string[] NamePrefixes { get; } = { };

        public IButtplugDevice CreateDevice(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
        {
            return new Vibratissimo(aLogManager, aInterface, this);
        }
    }

    internal class Vibratissimo : ButtplugBluetoothDevice
    {
        private double _vibratorSpeed;

        public Vibratissimo(IButtplugLogManager aLogManager,
                            IBluetoothDeviceInterface aInterface,
                            IBluetoothDeviceInfo aInfo)
            : base(aLogManager,
                   $"Vibratissimo Device ({aInterface.Name})",
                   aInterface,
                   aInfo)
        {
            MsgFuncs.Add(typeof(SingleMotorVibrateCmd), new ButtplugDeviceWrapper(HandleSingleMotorVibrateCmd));
            MsgFuncs.Add(typeof(VibrateCmd), new ButtplugDeviceWrapper(HandleVibrateCmd, new MessageAttributes() { FeatureCount = 1 }));
            MsgFuncs.Add(typeof(StopDeviceCmd), new ButtplugDeviceWrapper(HandleStopDeviceCmd));
        }

        private async Task<ButtplugMessage> HandleStopDeviceCmd(ButtplugDeviceMessage aMsg)
        {
            BpLogger.Debug("Stopping Device " + Name);
            return await HandleSingleMotorVibrateCmd(new SingleMotorVibrateCmd(aMsg.DeviceIndex, 0, aMsg.Id));
        }

        private async Task<ButtplugMessage> HandleSingleMotorVibrateCmd(ButtplugDeviceMessage aMsg)
        {
            if (!(aMsg is SingleMotorVibrateCmd cmdMsg))
            {
                return BpLogger.LogErrorMsg(aMsg.Id, Error.ErrorClass.ERROR_DEVICE, "Wrong Handler");
            }

            return await HandleVibrateCmd(VibrateCmd.Create(cmdMsg.DeviceIndex, cmdMsg.Id, cmdMsg.Speed, 1));
        }

        private async Task<ButtplugMessage> HandleVibrateCmd(ButtplugDeviceMessage aMsg)
        {
            if (!(aMsg is VibrateCmd cmdMsg))
            {
                return BpLogger.LogErrorMsg(aMsg.Id, Error.ErrorClass.ERROR_DEVICE, "Wrong Handler");
            }

            if (cmdMsg.Speeds.Count != 1)
            {
                return new Error(
                    "VibrateCmd requires 1 vector for this device.",
                    Error.ErrorClass.ERROR_DEVICE,
                    cmdMsg.Id);
            }

            foreach (var v in cmdMsg.Speeds)
            {
                if (v.Index != 0)
                {
                    return new Error(
                        $"Index {v.Index} is out of bounds for VibrateCmd for this device.",
                        Error.ErrorClass.ERROR_DEVICE,
                        cmdMsg.Id);
                }

                if (Math.Abs(v.Speed - _vibratorSpeed) < 0.001)
                {
                    return new Ok(cmdMsg.Id);
                }

                _vibratorSpeed = v.Speed;
            }

            var data = new byte[] { 0x03, 0xff };
            await Interface.WriteValue(aMsg.Id,
                (uint)VibratissimoBluetoothInfo.Chrs.TxMode,
                data);

            data[0] = Convert.ToByte(_vibratorSpeed * byte.MaxValue);
            data[1] = 0x00;
            return await Interface.WriteValue(aMsg.Id,
                (uint)VibratissimoBluetoothInfo.Chrs.TxSpeed,
                data);
        }
    }
}
