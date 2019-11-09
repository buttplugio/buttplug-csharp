// <copyright file="AnerosProtocol.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;

namespace Buttplug.Devices.Protocols
{
    internal class AnerosProtocol : ButtplugDeviceProtocol
    {
        private readonly double[] _vibratorSpeeds = { 0, 0 };

        public AnerosProtocol(IButtplugLogManager aLogManager,
                               IButtplugDeviceImpl aInterface)
            : base(aLogManager,
                   "Aneros Vivi",
                   aInterface)
        {
            AddMessageHandler<SingleMotorVibrateCmd>(HandleSingleMotorVibrateCmd);
            AddMessageHandler<VibrateCmd>(HandleVibrateCmd, new MessageAttributes() { FeatureCount = 2 });
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

            return await HandleVibrateCmd(VibrateCmd.Create(cmdMsg.DeviceIndex, cmdMsg.Id, cmdMsg.Speed, 2), aToken).ConfigureAwait(false);
        }

        private async Task<ButtplugMessage> HandleVibrateCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckGenericMessageHandler<VibrateCmd>(aMsg, 2);
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

            if (changed[0] || !SentVibration)
            {
                await Interface.WriteValueAsync(
                        new byte[] { 0xF1, (byte)Convert.ToUInt32(_vibratorSpeeds[0] * 0x7F) }, aToken)
                    .ConfigureAwait(false);
            }

            if (changed[1] || !SentVibration)
            {
                await Interface.WriteValueAsync(
                        new byte[] { 0xF2, (byte)Convert.ToUInt32(_vibratorSpeeds[1] * 0x7F) }, aToken)
                    .ConfigureAwait(false);
            }

            SentVibration = true;
            return new Ok(aMsg.Id);
        }
    }
}
