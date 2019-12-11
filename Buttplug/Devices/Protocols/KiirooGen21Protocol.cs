// <copyright file="KiirooGen21Protocol.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace Buttplug.Devices.Protocols
{
    // ReSharper disable once InconsistentNaming
    internal class KiirooGen21Protocol : ButtplugDeviceProtocol
    {
        private readonly double[] _vibratorSpeeds = { 0, 0, 0 };
        private double _lastPosition;

        // ReSharper disable once InconsistentNaming
        internal struct KiirooGen21Type
        {
            public string Brand;
            public string Name;
            public bool HasLinear;
            public uint VibeCount;
            public uint[] VibeOrder;
        }

        internal static readonly Dictionary<string, KiirooGen21Type> DevInfos = new Dictionary<string, KiirooGen21Type>()
        {
            {
                "Cliona",
                new KiirooGen21Type
                {
                    Brand = "Kiiroo",
                    Name = "Cliona",
                    HasLinear = false,
                    VibeCount = 1,
                    VibeOrder = new[] { 0u },
                }
            },
            {
                "Pearl2.1",
                new KiirooGen21Type
                {
                    Brand = "Kiiroo",
                    Name = "Pearl 2.1",
                    HasLinear = false,
                    VibeCount = 1,
                    VibeOrder = new[] { 0u },
                }
            },
            {
                "OhMiBod 4.0",
                new KiirooGen21Type
                {
                    Brand = "OhMiBod",
                    Name = "Esca 2",
                    HasLinear = false,
                    VibeCount = 1,
                    VibeOrder = new[] { 0u },
                }
            },
            {
                "Onyx2.1",
                new KiirooGen21Type
                {
                    Brand = "Kiiroo",
                    Name = "Onyx 2.1",
                    HasLinear = true,
                    VibeCount = 0,
                    VibeOrder = new uint[0],
                }
            },
            {
                "Titan1.1",
                new KiirooGen21Type
                {
                    Brand = "Kiiroo",
                    Name = "Titan 1.1",
                    HasLinear = true,
                    VibeCount = 1, // actually 3
                    VibeOrder = new[] { 0u },
                }
            },
        };

        private readonly KiirooGen21Type _devInfo;

        public KiirooGen21Protocol([NotNull] IButtplugLogManager aLogManager,
                      [NotNull] IButtplugDeviceImpl aInterface)
            : base(aLogManager,
                   "Kiiroo Unknown",
                   aInterface)
        {
            if (DevInfos.ContainsKey(aInterface.Name))
            {
                Name = $"{DevInfos[aInterface.Name].Brand} {DevInfos[aInterface.Name].Name}";
                _devInfo = DevInfos[aInterface.Name];
            }
            else
            {
                BpLogger.Warn($"Cannot identify device {Name}, defaulting to Pearl2 settings.");
                _devInfo = DevInfos["Pearl2.1"];
            }

            AddMessageHandler<StopDeviceCmd>(HandleStopDeviceCmd);

            if (_devInfo.VibeCount > 0)
            {
                AddMessageHandler<VibrateCmd>(HandleVibrateCmd,
                    new MessageAttributes { FeatureCount = _devInfo.VibeCount });
                AddMessageHandler<SingleMotorVibrateCmd>(HandleSingleMotorVibrateCmd);
            }

            if (_devInfo.HasLinear)
            {
                AddMessageHandler<LinearCmd>(HandleLinearCmd,
                    new MessageAttributes { FeatureCount = 1 });
                AddMessageHandler<FleshlightLaunchFW12Cmd>(HandleFleshlightLaunchFW12Cmd);
            }
        }

        private async Task<ButtplugMessage> HandleStopDeviceCmd([NotNull] ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            BpLogger.Debug($"Stopping Device {Name}");

            if (_devInfo.VibeCount == 0)
            {
                return new Ok(aMsg.Id);
            }

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

            if (!changed && SentVibration)
            {
                return new Ok(cmdMsg.Id);
            }

            SentVibration = true;

            var data = new[]
            {
                (byte)0x01,
                (byte)Convert.ToUInt16(_vibratorSpeeds[_devInfo.VibeOrder[0]] * 100),
            };

            await Interface.WriteValueAsync(data, aToken).ConfigureAwait(false);
            return new Ok(aMsg.Id);
        }

        private async Task<ButtplugMessage> HandleLinearCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckGenericMessageHandler<LinearCmd>(aMsg, 1);
            var v = cmdMsg.Vectors[0];

            // For now, generate speeds that are similar to the fleshlight
            // launch. The scale is actually far different, but this is better
            // than nothing.
            return await HandleFleshlightLaunchFW12Cmd(new FleshlightLaunchFW12Cmd(cmdMsg.DeviceIndex,
                Convert.ToUInt32(FleshlightHelper.GetSpeed(Math.Abs(_lastPosition - v.Position), v.Duration) * 99),
                Convert.ToUInt32(v.Position * 99), cmdMsg.Id), aToken).ConfigureAwait(false);
        }

        private async Task<ButtplugMessage> HandleFleshlightLaunchFW12Cmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckMessageHandler<FleshlightLaunchFW12Cmd>(aMsg);

            _lastPosition = Convert.ToDouble(cmdMsg.Position) / 99;
            // The Onyx2.1 is currently too stupid to drive its own motor
            // slowly. If we go below like, 5, it'll stall and require a reboot.
            // I hate you, Kiiroo. I hate you so much.
            var speed = Math.Max(6, cmdMsg.Speed);

            await Interface.WriteValueAsync(new[] { (byte)0x03, (byte)0x00, (byte)speed, (byte)cmdMsg.Position },
                aToken).ConfigureAwait(false);
            return new Ok(aMsg.Id);
        }
    }
}
