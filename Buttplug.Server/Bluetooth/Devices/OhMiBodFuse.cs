using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace Buttplug.Server.Bluetooth.Devices
{
    internal class OhMiBodFuseBluetoothInfo : IBluetoothDeviceInfo
    {
        public enum Chrs : uint
        {
            Tx = 0,
            RxTouch = 1,
            RxAccel = 2,
        }

        public string[] Names { get; } = { "Fuse" };

        public Guid[] Services { get; } = { new Guid("88f82580-0000-01e6-aace-0002a5d5c51b") };

        public Guid[] Characteristics { get; } =
        {
            // tx
            new Guid("88f82581-0000-01e6-aace-0002a5d5c51b"),

            // rx (touch)
            new Guid("88f82582-0000-01e6-aace-0002a5d5c51b"),

            // rx (accellorometer?)
            new Guid("88f82584-0000-01e6-aace-0002a5d5c51b"),
        };

        public IButtplugDevice CreateDevice(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
        {
            return new OhMiBodFuse(aLogManager, aInterface, this);
        }
    }

    internal class OhMiBodFuse : ButtplugBluetoothDevice
    {
        private readonly double[] _vibratorSpeeds = { 0, 0 };

        public OhMiBodFuse([NotNull] IButtplugLogManager aLogManager,
                      [NotNull] IBluetoothDeviceInterface aInterface,
                      [NotNull] IBluetoothDeviceInfo aInfo)
            : base(aLogManager,
                   $"OhMiBod {aInterface.Name}",
                   aInterface,
                   aInfo)
        {
            MsgFuncs.Add(typeof(StopDeviceCmd), new ButtplugDeviceWrapper(HandleStopDeviceCmd));
            MsgFuncs.Add(typeof(VibrateCmd), new ButtplugDeviceWrapper(HandleVibrateCmd, new MessageAttributes() { FeatureCount = 2 }));
            MsgFuncs.Add(typeof(SingleMotorVibrateCmd), new ButtplugDeviceWrapper(HandleSingleMotorVibrateCmd));
        }

        private async Task<ButtplugMessage> HandleStopDeviceCmd([NotNull] ButtplugDeviceMessage aMsg)
        {
            BpLogger.Debug("Stopping Device " + Name);
            return await HandleVibrateCmd(new VibrateCmd(aMsg.DeviceIndex,
                new List<VibrateCmd.VibrateSubcommand>()
                {
                    new VibrateCmd.VibrateSubcommand(0, 0),
                    new VibrateCmd.VibrateSubcommand(1, 0),
                },
                aMsg.Id));
        }

        private async Task<ButtplugMessage> HandleSingleMotorVibrateCmd([NotNull] ButtplugDeviceMessage aMsg)
        {
            if (!(aMsg is SingleMotorVibrateCmd cmdMsg))
            {
                return BpLogger.LogErrorMsg(aMsg.Id, Error.ErrorClass.ERROR_DEVICE, "Wrong Handler");
            }

            if (Math.Abs(_vibratorSpeeds[0] - cmdMsg.Speed) < 0.0001 && Math.Abs(_vibratorSpeeds[0] - cmdMsg.Speed) < 0.0001)
            {
                return new Ok(cmdMsg.Id);
            }

            return await HandleVibrateCmd(new VibrateCmd(cmdMsg.DeviceIndex,
                new List<VibrateCmd.VibrateSubcommand>()
                {
                    new VibrateCmd.VibrateSubcommand(0, cmdMsg.Speed),
                    new VibrateCmd.VibrateSubcommand(1, cmdMsg.Speed),
                },
                cmdMsg.Id));
        }

        private async Task<ButtplugMessage> HandleVibrateCmd([NotNull] ButtplugDeviceMessage aMsg)
        {
            if (!(aMsg is VibrateCmd cmdMsg))
            {
                return BpLogger.LogErrorMsg(aMsg.Id, Error.ErrorClass.ERROR_DEVICE, "Wrong Handler");
            }

            if (cmdMsg.Speeds.Count < 1 || cmdMsg.Speeds.Count > 2)
            {
                return new Error(
                    "VibrateCmd requires between 1 and 2 vectors for this device.",
                    Error.ErrorClass.ERROR_DEVICE,
                    cmdMsg.Id);
            }

            var changed = false;
            foreach (var vi in cmdMsg.Speeds)
            {
                if (vi.Index >= 2)
                {
                    return new Error(
                        $"Index {vi.Index} is out of bounds for VibrateCmd for this device.",
                        Error.ErrorClass.ERROR_DEVICE,
                        cmdMsg.Id);
                }

                if (Math.Abs(_vibratorSpeeds[vi.Index] - vi.Speed) < 0.0001)
                {
                    continue;
                }

                _vibratorSpeeds[vi.Index] = vi.Speed;
                changed = true;
            }

            if (!changed)
            {
                return new Ok(cmdMsg.Id);
            }

            var data = new[]
            {
                (byte)Convert.ToUInt16(_vibratorSpeeds[1] * 100),
                (byte)Convert.ToUInt16(_vibratorSpeeds[0] * 100),
                (byte)0x00,
            };

            return await Interface.WriteValue(aMsg.Id,
                Info.Characteristics[(uint)FleshlightLaunchBluetoothInfo.Chrs.Tx],
                data);
        }
    }
}
