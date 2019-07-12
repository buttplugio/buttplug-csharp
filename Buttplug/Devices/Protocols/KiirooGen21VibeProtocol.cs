// <copyright file="KiirooGen2VibeProtocol.cs" company="Nonpolynomial Labs LLC">
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
    internal class KiirooGen21VibeProtocol : ButtplugDeviceProtocol
    {
        private readonly double[] _vibratorSpeeds = { 0, 0, 0 };

        // ReSharper disable once InconsistentNaming
        internal struct KiirooGen21VibeType
        {
            public string Brand;
            public uint VibeCount;
            public uint[] VibeOrder;
        }

        internal static readonly Dictionary<string, KiirooGen21VibeType> DevInfos = new Dictionary<string, KiirooGen21VibeType>()
        {
            {
                "Cliona",
                new KiirooGen21VibeType
                {
                    Brand = "Kiiroo",
                    VibeCount = 1,
                    VibeOrder = new[] { 0u, 1u, 2u },
                }
            },
        };

        private KiirooGen21VibeType _devInfo;

        public KiirooGen21VibeProtocol([NotNull] IButtplugLogManager aLogManager,
                      [NotNull] IButtplugDeviceImpl aInterface)
            : base(aLogManager,
                   "Kiiroo Unknown",
                   aInterface)
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
    }
}
