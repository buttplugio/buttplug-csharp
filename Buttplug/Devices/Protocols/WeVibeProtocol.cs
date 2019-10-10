// <copyright file="WeVibe.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;

namespace Buttplug.Devices.Protocols
{
    internal class WeVibeProtocol : ButtplugDeviceProtocol
    {
        private static readonly string[] DualVibes =
        {
            "Cougar",
            "4 Plus",
            "4plus",
            "classic",
            "Classic",
            "Gala",
            "Nova",
            "NOVAV2",
            "Sync",
            "Vector",
        };

        private static readonly Dictionary<string, string> NameMap = new Dictionary<string, string>()
        {
            { "Cougar", "4 Plus" },
            { "4plus", "4 Plus" },
            { "classic", "Classic" },
            { "NOVAV2", "Nova" },
        };

        private static readonly string[] EightBitSpeed =
        {
            "Moxie",
            "Vector",
        };

        private readonly uint _vibratorCount = 1;
        private readonly bool _eightBitSpeed = false;
        private readonly double[] _vibratorSpeed = { 0, 0 };

        public WeVibeProtocol(IButtplugLogManager aLogManager,
                              IButtplugDeviceImpl aInterface)
            : base(aLogManager,
                   $"WeVibe {aInterface.Name}",
                   aInterface)
        {
            if (DualVibes.Contains(aInterface.Name))
            {
                _vibratorCount = 2;
            }

            if (NameMap.ContainsKey(aInterface.Name))
            {
                Name = $"WeVibe {NameMap[aInterface.Name]}";
            }

            if (EightBitSpeed.Contains(aInterface.Name))
            {
                _eightBitSpeed = true;
            }

            AddMessageHandler<SingleMotorVibrateCmd>(HandleSingleMotorVibrateCmd);
            AddMessageHandler<VibrateCmd>(HandleVibrateCmd, new MessageAttributes() { FeatureCount = _vibratorCount });
            AddMessageHandler<StopDeviceCmd>(HandleStopDeviceCmd);
        }

        private async Task<ButtplugMessage> HandleStopDeviceCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            return await HandleSingleMotorVibrateCmd(new SingleMotorVibrateCmd(aMsg.DeviceIndex, 0, aMsg.Id), aToken).ConfigureAwait(false);
        }

        private async Task<ButtplugMessage> HandleSingleMotorVibrateCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckMessageHandler<SingleMotorVibrateCmd>(aMsg);

            return await HandleVibrateCmd(VibrateCmd.Create(cmdMsg.DeviceIndex, cmdMsg.Id, cmdMsg.Speed, _vibratorCount), aToken).ConfigureAwait(false);
        }

        private async Task<ButtplugMessage> HandleVibrateCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckGenericMessageHandler<VibrateCmd>(aMsg, _vibratorCount);

            var changed = false;
            foreach (var v in cmdMsg.Speeds)
            {
                if (!(Math.Abs(v.Speed - _vibratorSpeed[v.Index]) > 0.001))
                {
                    continue;
                }

                changed = true;
                _vibratorSpeed[v.Index] = v.Speed;
            }

            if (!changed && SentVibration)
            {
                return new Ok(cmdMsg.Id);
            }

            SentVibration = true;

            // 0f 03 00 bc 00 00 00 00
            var data = new byte[] { 0x0f, 0x03, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00 };
            var rSpeedInt = 0;
            var rSpeedExt = 0;

            if (_eightBitSpeed)
            {
                rSpeedInt = Convert.ToUInt16(_vibratorSpeed[0] * 12);
                rSpeedExt = Convert.ToUInt16(_vibratorSpeed[_vibratorCount - 1] * 12);

                data[3] = Convert.ToByte(rSpeedExt + 3); // External
                data[4] = Convert.ToByte(rSpeedInt + 3); // Internal
                data[5] = Convert.ToByte(rSpeedInt == 0 ? 0 : 1);
                data[5] |= Convert.ToByte(rSpeedExt == 0 ? 0 : 2);
            }
            else
            {
                rSpeedInt = Convert.ToUInt16(_vibratorSpeed[0] * 15);
                rSpeedExt = Convert.ToUInt16(_vibratorSpeed[_vibratorCount - 1] * 15);

                data[3] = Convert.ToByte(rSpeedExt); // External
                data[3] |= Convert.ToByte(rSpeedInt << 4); // Internal
            }

            // ReSharper disable once InvertIf
            if (rSpeedInt == 0 && rSpeedExt == 0)
            {
                data[1] = 0x00;
                data[3] = 0x00;
                data[4] = 0x00;
                data[5] = 0x00;
            }

            await Interface.WriteValueAsync(data, aToken).ConfigureAwait(false);
            return new Ok(aMsg.Id);
        }
    }
}
