using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;
using Buttplug.Server.Util;
using JetBrains.Annotations;

namespace Buttplug.Server.Bluetooth.Devices
{
    internal class FleshlightLaunchBluetoothInfo : IBluetoothDeviceInfo
    {
        public enum Chrs : uint
        {
            Tx = 0,
            Rx = 1,
            Cmd = 2,
        };

        public Dictionary<uint, Guid> Characteristics { get; } = new Dictionary<uint, Guid>()
        {
            // tx
            { (uint)Chrs.Tx, new Guid("88f80581-0000-01e6-aace-0002a5d5c51b") },

            // rx
            { (uint)Chrs.Rx, new Guid("88f80582-0000-01e6-aace-0002a5d5c51b") },

            // cmd
            { (uint)Chrs.Cmd, new Guid("88f80583-0000-01e6-aace-0002a5d5c51b") },
        };

        public Guid[] Services { get; } = { new Guid("88f80580-0000-01e6-aace-0002a5d5c51b") };

        public string[] Names { get; } = { "Launch" };

        public string[] NamePrefixes { get; } = { };

        public IButtplugDevice CreateDevice(
            IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
        {
            return new FleshlightLaunch(aLogManager, aInterface, this);
        }
    }

    internal class FleshlightLaunch : ButtplugBluetoothDevice
    {
        private double _lastPosition;

        public FleshlightLaunch([NotNull] IButtplugLogManager aLogManager,
                                [NotNull] IBluetoothDeviceInterface aInterface,
                                [NotNull] IBluetoothDeviceInfo aInfo)
            : base(aLogManager,
                   "Fleshlight Launch",
                   aInterface,
                   aInfo)
        {
            // Setup message function array
            MsgFuncs.Add(typeof(FleshlightLaunchFW12Cmd), new ButtplugDeviceWrapper(HandleFleshlightLaunchRawCmd));
            MsgFuncs.Add(typeof(LinearCmd), new ButtplugDeviceWrapper(HandleLinearCmd, new MessageAttributes() { FeatureCount = 1 }));
            MsgFuncs.Add(typeof(StopDeviceCmd), new ButtplugDeviceWrapper(HandleStopDeviceCmd));
        }

        public override async Task<ButtplugMessage> Initialize()
        {
            BpLogger.Trace($"Initializing {Name}");
            return await Interface.WriteValue(ButtplugConsts.SystemMsgId,
                (int)FleshlightLaunchBluetoothInfo.Chrs.Tx,
                new byte[] { 0 },
                true);
        }

        private Task<ButtplugMessage> HandleStopDeviceCmd(ButtplugDeviceMessage aMsg)
        {
            // This probably shouldn't be a nop, but right now we don't have a good way to know
            // if the launch is moving or not, and surprisingly enough, setting speed to 0 does not
            // actually stop movement. It just makes it move really slow.
            // However, since each move it makes is finite (unlike setting vibration on some devices),
            // so we can assume it will be a short move, similar to what we do for the Kiiroo toys.
            BpLogger.Debug("Stopping Device " + Name);
            return Task.FromResult<ButtplugMessage>(new Ok(aMsg.Id));
        }

        private async Task<ButtplugMessage> HandleLinearCmd(ButtplugDeviceMessage aMsg)
        {
            var cmdMsg = aMsg as LinearCmd;
            if (cmdMsg is null)
            {
                return BpLogger.LogErrorMsg(aMsg.Id, Error.ErrorClass.ERROR_DEVICE, "Wrong Handler");
            }

            if (cmdMsg.Vectors.Count != 1)
            {
                return new Error(
                    "LinearCmd requires 1 vector for this device.",
                    Error.ErrorClass.ERROR_DEVICE,
                    cmdMsg.Id);
            }

            foreach (var v in cmdMsg.Vectors)
            {
                if (v.Index != 0)
                {
                    return new Error(
                        $"Index {v.Index} is out of bounds for LinearCmd for this device.",
                        Error.ErrorClass.ERROR_DEVICE,
                        cmdMsg.Id);
                }

                return await HandleFleshlightLaunchRawCmd(new FleshlightLaunchFW12Cmd(cmdMsg.DeviceIndex,
                    Convert.ToUInt32(FleshlightHelper.GetSpeed(Math.Abs(_lastPosition - v.Position), v.Duration) * 99),
                    Convert.ToUInt32(v.Position * 99), cmdMsg.Id));
            }

            return new Ok(aMsg.Id);
        }

        private async Task<ButtplugMessage> HandleFleshlightLaunchRawCmd(ButtplugDeviceMessage aMsg)
        {
            // TODO: Split into Command message and Control message? (Issue #17)
            if (!(aMsg is FleshlightLaunchFW12Cmd cmdMsg))
            {
                return BpLogger.LogErrorMsg(aMsg.Id, Error.ErrorClass.ERROR_DEVICE, "Wrong Handler");
            }

            _lastPosition = Convert.ToDouble(cmdMsg.Position) / 99;

            return await Interface.WriteValue(aMsg.Id,
                (int)FleshlightLaunchBluetoothInfo.Chrs.Tx,
                new[] { (byte)cmdMsg.Position, (byte)cmdMsg.Speed });
        }
    }
}
