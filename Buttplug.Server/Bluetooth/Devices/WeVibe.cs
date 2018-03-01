﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;

namespace Buttplug.Server.Bluetooth.Devices
{
    internal class WeVibeBluetoothInfo : IBluetoothDeviceInfo
    {
        public enum Chrs : uint
        {
            Tx = 0,
            Rx,
        }

        public Guid[] Services { get; } = { new Guid("f000bb03-0451-4000-b000-000000000000") };

        public string[] Names { get; } =
        {
            "Cougar",
            "4 Plus",
            "4plus",
            "Bloom",
            "classic",
            "Ditto",
            "Gala",
            "Jive",
            "Nova",
            "NOVAV2",
            "Pivot",
            "Rave",
            "Sync",
            "Verge",
            "Wish",
        };

        public Guid[] Characteristics { get; } =
        {
            // tx characteristic
            new Guid("f000c000-0451-4000-b000-000000000000"),

            // rx characteristic
            new Guid("f000b000-0451-4000-b000-000000000000"),
        };

        public IButtplugDevice CreateDevice(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
        {
            return new WeVibe(aLogManager, aInterface, this);
        }
    }

    internal class WeVibe : ButtplugBluetoothDevice
    {
        private static readonly string[] DualVibes =
        {
            "Cougar",
            "4 Plus",
            "4plus",
            "classic",
            "Gala",
            "Nova",
            "NOVAV2",
            "Sync",
        };

        private readonly uint _vibratorCount = 1;
        private readonly double[] _vibratorSpeed = { 0, 0 };

        public WeVibe(IButtplugLogManager aLogManager,
                      IBluetoothDeviceInterface aInterface,
                      IBluetoothDeviceInfo aInfo)
            : base(aLogManager,
                   $"WeVibe Device ({aInterface.Name})",
                   aInterface,
                   aInfo)
        {
            if (DualVibes.Contains(aInterface.Name))
            {
                _vibratorCount = 2;
            }

            MsgFuncs.Add(typeof(SingleMotorVibrateCmd), new ButtplugDeviceWrapper(HandleSingleMotorVibrateCmd));
            MsgFuncs.Add(typeof(VibrateCmd), new ButtplugDeviceWrapper(HandleVibrateCmd, new MessageAttributes() { FeatureCount = _vibratorCount }));
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

            var rSpeedInt = Convert.ToUInt16(_vibratorSpeed[0] * 15);
            var rSpeedExt = Convert.ToUInt16(_vibratorSpeed[_vibratorCount - 1] * 15);

            // 0f 03 00 bc 00 00 00 00
            var data = new byte[] { 0x0f, 0x03, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00 };
            data[3] = Convert.ToByte(rSpeedExt); // External
            data[3] |= Convert.ToByte(rSpeedInt << 4); // Internal

            // ReSharper disable once InvertIf
            if (rSpeedInt == 0 && rSpeedExt == 0)
            {
                data[1] = 0x00;
                data[5] = 0x00;
            }

            return await Interface.WriteValue(aMsg.Id,
                Info.Characteristics[(uint)WeVibeBluetoothInfo.Chrs.Tx],
                data);
        }
    }
}
