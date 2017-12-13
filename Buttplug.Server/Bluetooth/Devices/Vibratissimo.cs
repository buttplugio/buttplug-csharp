using System;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;
using System.Collections.Generic;

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

        public Guid[] Characteristics { get; } =
        {
            // tx characteristic
            new Guid("00001524-1212-efde-1523-785feabcd123"),
            new Guid("00001526-1212-efde-1523-785feabcd123"),

            // rx characteristic
            new Guid("00001527-1212-efde-1523-785feabcd123"),
        };

        public IButtplugDevice CreateDevice(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
        {
            return new Vibratissimo(aLogManager, aInterface, this);
        }
    }

    internal class Vibratissimo : ButtplugBluetoothDevice
    {
        private double _vibratorSpeed = 0;

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
            var cmdMsg = aMsg as SingleMotorVibrateCmd;
            if (cmdMsg is null)
            {
                return BpLogger.LogErrorMsg(aMsg.Id, Error.ErrorClass.ERROR_DEVICE, "Wrong Handler");
            }

            return await HandleVibrateCmd(new VibrateCmd(cmdMsg.DeviceIndex,
                new List<VibrateCmd.VibrateIndex>() { new VibrateCmd.VibrateIndex(0, cmdMsg.Speed) },
                cmdMsg.Id));
        }

        private async Task<ButtplugMessage> HandleVibrateCmd(ButtplugDeviceMessage aMsg)
        {
            var cmdMsg = aMsg as VibrateCmd;
            if (cmdMsg is null)
            {
                return BpLogger.LogErrorMsg(aMsg.Id, Error.ErrorClass.ERROR_DEVICE, "Wrong Handler");
            }

            foreach (var vi in cmdMsg.Speeds)
            {
                if (vi.Index == 0)
                {
                    if (vi.Speed == _vibratorSpeed)
                    {
                        return new Ok(cmdMsg.Id);
                    }

                    _vibratorSpeed = vi.Speed;
                }
            }

            var data = new byte[2];
            data[0] = 0x03;
            data[1] = 0xFF;
            await Interface.WriteValue(aMsg.Id,
                Info.Characteristics[(uint)VibratissimoBluetoothInfo.Chrs.TxMode],
                data);

            data[0] = Convert.ToByte(_vibratorSpeed * byte.MaxValue);
            data[1] = 0x00;
            return await Interface.WriteValue(aMsg.Id,
                Info.Characteristics[(uint)VibratissimoBluetoothInfo.Chrs.TxSpeed],
                data);
        }
    }
}
