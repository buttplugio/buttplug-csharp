// <copyright file="KiirooOnyx21Protocol.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace Buttplug.Devices.Protocols
{
    internal class KiirooGen21Protocol : ButtplugDeviceProtocol
    {
        private double _lastPosition;

        public KiirooGen21Protocol([NotNull] IButtplugLogManager aLogManager,
            IButtplugDeviceImpl aInterface)
            : base(aLogManager,
                "Kiiroo Onyx2.1",
                aInterface)
        {
            // Setup message function array
            AddMessageHandler<FleshlightLaunchFW12Cmd>(HandleFleshlightLaunchFW12Cmd);
            AddMessageHandler<LinearCmd>(HandleLinearCmd, new MessageAttributes() { FeatureCount = 1 });
            AddMessageHandler<StopDeviceCmd>(HandleStopDeviceCmd);
        }

        private Task<ButtplugMessage> HandleStopDeviceCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            BpLogger.Debug("Stopping Device " + Name);
            return Task.FromResult<ButtplugMessage>(new Ok(aMsg.Id));
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
