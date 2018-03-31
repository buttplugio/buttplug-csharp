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
        public Guid[] Services { get; } = { new Guid("0000fee9-0000-1000-8000-00805f9b34fb") };

        public string[] NamePrefixes { get; } = { };

        public string[] Names { get; } =
        {
            // Warrior II
            "Youcups",
        };

        public Dictionary<uint, Guid> Characteristics { get; } = new Dictionary<uint, Guid>();

        public IButtplugDevice CreateDevice(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
        {
            return new Youcups(aLogManager, aInterface, this);
        }
    }

    internal class Youcups : ButtplugBluetoothDevice
    {
        private static readonly Dictionary<string, string> FriendlyNames = new Dictionary<string, string>
        {
            { "Youcups", "Warrior II" },
        };

        private double _vibratorSpeed = 0;

        public Youcups(IButtplugLogManager aLogManager,
                       IBluetoothDeviceInterface aInterface,
                       IBluetoothDeviceInfo aInfo)
            : base(aLogManager,
                   $"Youcups Unknown",
                   aInterface,
                   aInfo)
        {
            if (FriendlyNames.ContainsKey(aInterface.Name))
            {
                Name = $"Youcups {FriendlyNames[aInterface.Name]}";
            }
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

            return await Interface.WriteValue(aMsg.Id,
                Encoding.ASCII.GetBytes($"$SYS,{(int)(_vibratorSpeed * 8), 1}?"));
        }
    }
}
