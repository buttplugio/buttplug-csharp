﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace Buttplug.Server.Bluetooth.Devices
{
    // ReSharper disable once InconsistentNaming
    internal class KiirooGen2VibeBluetoothInfo : IBluetoothDeviceInfo
    {
        public enum Chrs : uint
        {
            Tx = 0,
            RxTouch = 1,
            RxAccel = 2,
        }

        public static string[] NamesInfo =
        {
            "Pearl2",
            "Fuse",
            "Virtual Blowbot",
        };

        public string[] Names { get; } = NamesInfo;

        public string[] NamePrefixes { get; } = { };

        public Guid[] Services { get; } = { new Guid("88f82580-0000-01e6-aace-0002a5d5c51b") };

        public Dictionary<uint, Guid> Characteristics { get; } = new Dictionary<uint, Guid>()
        {
            // tx
            { (uint)Chrs.Tx, new Guid("88f82581-0000-01e6-aace-0002a5d5c51b") },

            // rx (touch: 3 zone bitmask)
            { (uint)Chrs.RxTouch, new Guid("88f82582-0000-01e6-aace-0002a5d5c51b") },

            // rx (accellorometer?)
            { (uint)Chrs.RxAccel, new Guid("88f82584-0000-01e6-aace-0002a5d5c51b") },
        };

        public IButtplugDevice CreateDevice(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
        {
            return new KiirooGen2Vibe(aLogManager, aInterface, this);
        }
    }

    // ReSharper disable once InconsistentNaming
    internal class KiirooGen2Vibe : ButtplugBluetoothDevice
    {
        private readonly double[] _vibratorSpeeds = { 0, 0, 0 };

        // ReSharper disable once InconsistentNaming
        internal struct KiirooGen2VibeType
        {
            public string Brand;
            public uint VibeCount;
            public uint[] VibeOrder;
        }

        internal static readonly Dictionary<string, KiirooGen2VibeType> DevInfos = new Dictionary<string, KiirooGen2VibeType>()
        {
            {
                "Pearl2",
                new KiirooGen2VibeType
                {
                    Brand = "Kiiroo",
                    VibeCount = 1,
                    VibeOrder = new[] { 0u, 1u, 2u },
                }
            },
            {
                "Fuse",
                new KiirooGen2VibeType
                {
                    Brand = "OhMiBod",
                    VibeCount = 2,
                    VibeOrder = new[] { 1u, 0u, 2u },
                }
            },
            {
                "Virtual Blowbot",
                new KiirooGen2VibeType
                {
                    Brand = "PornHub",
                    VibeCount = 3,
                    VibeOrder = new[] { 0u, 1u, 2u },
                }
            },
        };

        private KiirooGen2VibeType _devInfo;

        public KiirooGen2Vibe([NotNull] IButtplugLogManager aLogManager,
                      [NotNull] IBluetoothDeviceInterface aInterface,
                      [NotNull] IBluetoothDeviceInfo aInfo)
            : base(aLogManager,
                   "Kiiroo Unknown",
                   aInterface,
                   aInfo)
        {
            if (DevInfos.ContainsKey(aInterface.Name))
            {
                Name = $"{DevInfos[aInterface.Name].Brand} {aInterface.Name}";
                _devInfo = DevInfos[aInterface.Name];
            }
            else
            {
                BpLogger.Warn($"Cannot identify device {Name}, defaulting to Pearl2 settings.");
                _devInfo = DevInfos["Unknown"];
            }

            MsgFuncs.Add(typeof(StopDeviceCmd), new ButtplugDeviceWrapper(HandleStopDeviceCmd));
            MsgFuncs.Add(typeof(VibrateCmd), new ButtplugDeviceWrapper(HandleVibrateCmd, new MessageAttributes { FeatureCount = _devInfo.VibeCount }));
            MsgFuncs.Add(typeof(SingleMotorVibrateCmd), new ButtplugDeviceWrapper(HandleSingleMotorVibrateCmd));
        }

        private async Task<ButtplugMessage> HandleStopDeviceCmd([NotNull] ButtplugDeviceMessage aMsg)
        {
            BpLogger.Debug($"Stopping Device {Name}");

            return await HandleVibrateCmd(VibrateCmd.Create(aMsg.DeviceIndex, aMsg.Id, 0, _devInfo.VibeCount));
        }

        private async Task<ButtplugMessage> HandleSingleMotorVibrateCmd([NotNull] ButtplugDeviceMessage aMsg)
        {
            if (!(aMsg is SingleMotorVibrateCmd cmdMsg))
            {
                return BpLogger.LogErrorMsg(aMsg.Id, Error.ErrorClass.ERROR_DEVICE, "Wrong Handler");
            }

            return await HandleVibrateCmd(VibrateCmd.Create(aMsg.DeviceIndex, aMsg.Id, cmdMsg.Speed, _devInfo.VibeCount));
        }

        private async Task<ButtplugMessage> HandleVibrateCmd([NotNull] ButtplugDeviceMessage aMsg)
        {
            if (!(aMsg is VibrateCmd cmdMsg))
            {
                return BpLogger.LogErrorMsg(aMsg.Id, Error.ErrorClass.ERROR_DEVICE, "Wrong Handler");
            }

            if (cmdMsg.Speeds.Count < 1 || cmdMsg.Speeds.Count > _devInfo.VibeCount)
            {
                return new Error(
                    $"VibrateCmd requires between 1 and {_devInfo.VibeCount} vectors for this device.",
                    Error.ErrorClass.ERROR_DEVICE,
                    cmdMsg.Id);
            }

            var changed = false;
            foreach (var vi in cmdMsg.Speeds)
            {
                if (vi.Index >= _devInfo.VibeCount)
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
                (byte)Convert.ToUInt16(_vibratorSpeeds[_devInfo.VibeOrder[0]] * 100),
                (byte)Convert.ToUInt16(_vibratorSpeeds[_devInfo.VibeOrder[1]] * 100),
                (byte)Convert.ToUInt16(_vibratorSpeeds[_devInfo.VibeOrder[2]] * 100),
            };

            return await Interface.WriteValue(aMsg.Id,
                (uint)FleshlightLaunchBluetoothInfo.Chrs.Tx,
                data);
        }
    }
}