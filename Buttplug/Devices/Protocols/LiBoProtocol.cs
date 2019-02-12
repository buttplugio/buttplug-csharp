// <copyright file="LiBoProtocol.cs" company="Nonpolynomial Labs LLC">
//     Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
//     Copyright (c) Nonpolynomial Labs LLC. All rights reserved. Licensed under the BSD 3-Clause
//     license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;

namespace Buttplug.Devices.Protocols
{
    internal class LiBoProtocol : ButtplugDeviceProtocol
    {
        private readonly uint _vibratorCount = 1;
        private readonly double[] _vibratorSpeed = { 0 };

        public LiBoProtocol(IButtplugLogManager aLogManager,
                      IButtplugDeviceImpl aInterface)
            : base(aLogManager,
                   $"LiBo ({aInterface.Name})",
                   aInterface)
        {
            AddMessageHandler<SingleMotorVibrateCmd>(HandleSingleMotorVibrateCmd);
            AddMessageHandler<VibrateCmd>(HandleVibrateCmd, new MessageAttributes() { FeatureCount = _vibratorCount });
            AddMessageHandler<StopDeviceCmd>(HandleStopDeviceCmd);

            // TODO Add a handler for Estim shocking, add a battery handler.
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

            // Map a 0 - 100% value to a 0 - 3 value since 0 * x == 0 this will turn off the vibe if
            // speed is 0.00
            var mode = (int)Math.Ceiling(_vibratorSpeed[0] * 3);

            var data = new[] { Convert.ToByte(mode) };

            await Interface.WriteValueAsync(data,
                new ButtplugDeviceWriteOptions { Endpoint = Endpoints.TxVibrate },
                aToken).ConfigureAwait(false);
            return new Ok(aMsg.Id);
        }
    }
}
