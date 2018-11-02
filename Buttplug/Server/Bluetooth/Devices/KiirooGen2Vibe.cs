// <copyright file="KiirooGen2Vibe.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Devices;
using Buttplug.Core.Logging;
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
            "Titan",
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

            // rx (accelerometer?)
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
            {
                "Titan",
                new KiirooGen2VibeType
                {
                    Brand = "Kiiroo",
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

            AddMessageHandler<StopDeviceCmd>(HandleStopDeviceCmd);
            AddMessageHandler<VibrateCmd>(HandleVibrateCmd, new MessageAttributes { FeatureCount = _devInfo.VibeCount });
            AddMessageHandler<SingleMotorVibrateCmd>(HandleSingleMotorVibrateCmd);
        }

        private async Task<ButtplugMessage> HandleStopDeviceCmd([NotNull] ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            BpLogger.Debug($"Stopping Device {Name}");

            return await HandleVibrateCmd(VibrateCmd.Create(aMsg.DeviceIndex, aMsg.Id, 0, _devInfo.VibeCount), aToken).ConfigureAwait(false);
        }

        private async Task<ButtplugMessage> HandleSingleMotorVibrateCmd([NotNull] ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckMessageHandler<SingleMotorVibrateCmd>(aMsg);

            return await HandleVibrateCmd(VibrateCmd.Create(aMsg.DeviceIndex, aMsg.Id, cmdMsg.Speed, _devInfo.VibeCount), aToken).ConfigureAwait(false);
        }

        private async Task<ButtplugMessage> HandleVibrateCmd([NotNull] ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckGenericMessageHandler<VibrateCmd>(aMsg, _devInfo.VibeCount);

            var changed = false;
            foreach (var vi in cmdMsg.Speeds)
            {
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

            return await Interface.WriteValueAsync(aMsg.Id,
                (uint)FleshlightLaunchBluetoothInfo.Chrs.Tx,
                data, false, aToken).ConfigureAwait(false);
        }
    }
}