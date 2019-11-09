// <copyright file="LovehoneyDesireProtocol.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;

namespace Buttplug.Devices.Protocols
{
    internal class LovehoneyDesireProtocol : ButtplugDeviceProtocol
    {
        private readonly double[] _vibratorSpeeds = { 0, 0 };

        internal struct LovehoneyDesireType
        {
            public string Name;
            public uint VibeCount;
        }

        internal readonly Dictionary<string, LovehoneyDesireType> _deviceMap = new Dictionary<string, LovehoneyDesireType>()
        {
            { "PROSTATE VIBE", new LovehoneyDesireType() { Name = "Prostate Vibrator", VibeCount = 2 } },
            { "KNICKER VIBE", new LovehoneyDesireType() { Name = "Knicker Vibrator", VibeCount = 1 } },
        };

        private readonly LovehoneyDesireType _devInfo;

        public LovehoneyDesireProtocol(IButtplugLogManager aLogManager,
                               IButtplugDeviceImpl aInterface)
            : base(aLogManager,
                   "Lovehoney Desire Device",
                   aInterface)
        {
            if (_deviceMap.TryGetValue(aInterface.Name, out var dev))
            {
                _devInfo = dev;
                Name = $"Lovehoney Desire {dev.Name}";
            }
            else
            {
                _devInfo = new LovehoneyDesireType()
                {
                    Name = "Unknown Lovehoney Desire Device",
                    VibeCount = 1,
                };
            }

            AddMessageHandler<SingleMotorVibrateCmd>(HandleSingleMotorVibrateCmd);
            AddMessageHandler<VibrateCmd>(HandleVibrateCmd, new MessageAttributes() { FeatureCount = _devInfo.VibeCount });
            AddMessageHandler<StopDeviceCmd>(HandleStopDeviceCmd);
        }

        private async Task<ButtplugMessage> HandleStopDeviceCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            BpLogger.Debug("Stopping Device " + Name);
            return await HandleSingleMotorVibrateCmd(new SingleMotorVibrateCmd(aMsg.DeviceIndex, 0, aMsg.Id), aToken).ConfigureAwait(false);
        }

        private async Task<ButtplugMessage> HandleSingleMotorVibrateCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckMessageHandler<SingleMotorVibrateCmd>(aMsg);

            return await HandleVibrateCmd(VibrateCmd.Create(cmdMsg.DeviceIndex, cmdMsg.Id, cmdMsg.Speed, _devInfo.VibeCount), aToken).ConfigureAwait(false);
        }

        private async Task<ButtplugMessage> HandleVibrateCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckGenericMessageHandler<VibrateCmd>(aMsg, _devInfo.VibeCount);
            var changed = new[] { false, false };

            foreach (var v in cmdMsg.Speeds)
            {
                if (Math.Abs(v.Speed - _vibratorSpeeds[v.Index]) < 0.001)
                {
                    continue;
                }

                changed[v.Index] = true;
                _vibratorSpeeds[v.Index] = v.Speed;
            }

            if (_devInfo.VibeCount >= 2 &&
                Math.Abs(_vibratorSpeeds[0] - _vibratorSpeeds[1]) < 0.001 &&
                (changed[0] || changed[1] || !SentVibration))
            {
                // Values are the same, so use global setter
                await Interface.WriteValueAsync(
                        new byte[] { 0xF3, 0, (byte)Convert.ToUInt32(_vibratorSpeeds[0] * 0x7F) }, aToken)
                    .ConfigureAwait(false);

                SentVibration = true;
                return new Ok(aMsg.Id);
            }

            if (changed[0] || !SentVibration)
            {
                await Interface.WriteValueAsync(
                        new byte[] { 0xF3, 1, (byte)Convert.ToUInt32(_vibratorSpeeds[0] * 0x7F) }, aToken)
                    .ConfigureAwait(false);
            }

            if (_devInfo.VibeCount >= 2 && (changed[1] || !SentVibration))
            {
                await Interface.WriteValueAsync(
                        new byte[] { 0xF3, 2, (byte)Convert.ToUInt32(_vibratorSpeeds[1] * 0x7F) }, aToken)
                    .ConfigureAwait(false);
            }

            SentVibration = true;
            return new Ok(aMsg.Id);
        }
    }
}
