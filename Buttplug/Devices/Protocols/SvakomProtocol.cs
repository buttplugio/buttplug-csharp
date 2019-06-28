// <copyright file="SvakomProtocol.cs" company="Nonpolynomial Labs LLC">
//     Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
//     Copyright (c) Nonpolynomial Labs LLC. All rights reserved. Licensed under the BSD 3-Clause
//     license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;

namespace Buttplug.Devices.Protocols
{
    internal class SvakomProtocol : ButtplugDeviceProtocol
    {
        private double _vibratorSpeed;

        public SvakomProtocol(IButtplugLogManager aLogManager,
                      IButtplugDeviceImpl aInterface)
            : base(aLogManager,
                   $"Svakom Ella",
                   aInterface)
        {
            AddMessageHandler<StopDeviceCmd>(HandleStopDeviceCmd);
            AddMessageHandler<SingleMotorVibrateCmd>(HandleSingleMotorVibrateCmd);
            AddMessageHandler<VibrateCmd>(HandleVibrateCmd,
                new MessageAttributes { FeatureCount = 1 });
        }

        private async Task<ButtplugMessage> HandleStopDeviceCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            return await HandleSingleMotorVibrateCmd(new SingleMotorVibrateCmd(aMsg.DeviceIndex, 0, aMsg.Id), aToken).ConfigureAwait(false);
        }

        private async Task<ButtplugMessage> HandleSingleMotorVibrateCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckMessageHandler<SingleMotorVibrateCmd>(aMsg);

            return await HandleVibrateCmd(VibrateCmd.Create(cmdMsg.DeviceIndex, cmdMsg.Id, cmdMsg.Speed, 1), aToken).ConfigureAwait(false);
        }

        private async Task<ButtplugMessage> HandleVibrateCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckGenericMessageHandler<VibrateCmd>(aMsg, 1);

            var changed = false;

            foreach (var v in cmdMsg.Speeds)
            {
                if (!(Math.Abs(v.Speed - _vibratorSpeed) > 0.001))
                {
                    continue;
                }

                changed = true;
                _vibratorSpeed = v.Speed;
            }

            if (!changed && SentVibration)
            {
                return new Ok(aMsg.Id);
            }

            var speed = Convert.ToByte((uint)Math.Ceiling(_vibratorSpeed * 0x13));
            var multiplier = speed == 0x00 ? (byte)0x00 : (byte)0x01;

            await Interface.WriteValueAsync(
                new[] { (byte)0x55, (byte)0x04, (byte)0x03, (byte)0x00, multiplier, speed },
                new ButtplugDeviceWriteOptions { Endpoint = Endpoints.Tx },
                aToken).ConfigureAwait(false);

            return new Ok(aMsg.Id);
        }
    }
}
