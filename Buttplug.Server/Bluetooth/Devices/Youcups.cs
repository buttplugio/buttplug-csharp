using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;

namespace Buttplug.Server.Bluetooth.Devices
{
    internal class YoucupsBluetoothInfo : IBluetoothDeviceInfo
    {
        public enum Chrs : uint
        {
            Tx = 0,
            Rx,
        }

        public Guid[] Services { get; } = { new Guid("0000fee9-0000-1000-8000-00805f9b34fb") };

        public string[] Names { get; } =
        {
            // Warrior II
            "Youcups",
        };

        public Guid[] Characteristics { get; } =
        {
            // tx characteristic
            new Guid("d44bc439-abfd-45a2-b575-925416129600"), // ,

            // rx characteristic
        };

        public IButtplugDevice CreateDevice(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
        {
            return new Youcups(aLogManager, aInterface, this);
        }
    }

    internal class Youcups : ButtplugBluetoothDevice
    {
        private static Dictionary<string, string> friendlyNames = new Dictionary<string, string>()
        {
            { "Youcups", "Warrior II" },
        };

        private double _vibratorSpeed = 0;

        public Youcups(IButtplugLogManager aLogManager,
                       IBluetoothDeviceInterface aInterface,
                       IBluetoothDeviceInfo aInfo)
            : base(aLogManager,
                   $"Youcups Device ({friendlyNames[aInterface.Name]})",
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
                new List<VibrateCmd.VibrateSubcommand>() { new VibrateCmd.VibrateSubcommand(0, cmdMsg.Speed) },
                cmdMsg.Id));
        }

        private async Task<ButtplugMessage> HandleVibrateCmd(ButtplugDeviceMessage aMsg)
        {
            var cmdMsg = aMsg as VibrateCmd;
            if (cmdMsg is null)
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

                if (v.Speed == _vibratorSpeed)
                {
                    return new Ok(cmdMsg.Id);
                }

                _vibratorSpeed = v.Speed;
            }

            return await Interface.WriteValue(aMsg.Id,
                Info.Characteristics[(uint)YoucupsBluetoothInfo.Chrs.Tx],
                Encoding.ASCII.GetBytes($"$SYS,{(int)(_vibratorSpeed * 8), 1}?"));
        }
    }
}
