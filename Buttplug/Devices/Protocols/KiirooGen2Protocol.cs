// <copyright file="KiirooGen2Protocol.cs" company="Nonpolynomial Labs LLC">
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
    /// <summary>
    ///  Protocol for the fleshlight launch and Onyx 2.
    /// </summary>
    internal class KiirooGen2Protocol : ButtplugDeviceProtocol
    {
        private static Dictionary<string, string> _brandNames = new Dictionary<string, string>
        {
            { "Launch", "Fleshlight" },
            { "Onyx2", "Kiiroo" },
        };

        private double _lastPosition;

        public KiirooGen2Protocol([NotNull] IButtplugLogManager aLogManager,
            IButtplugDeviceImpl aInterface)
            : base(aLogManager,
                "Kiiroo V2 Protocol Device",
                aInterface)
        {
            if (_brandNames.ContainsKey(aInterface.Name))
            {
                Name = $"{_brandNames[aInterface.Name]} {aInterface.Name}";
            }

            // Setup message function array
            AddMessageHandler<FleshlightLaunchFW12Cmd>(HandleFleshlightLaunchFW12Cmd);
            AddMessageHandler<LinearCmd>(HandleLinearCmd, new MessageAttributes() { FeatureCount = 1 });
            AddMessageHandler<StopDeviceCmd>(HandleStopDeviceCmd);
        }

        public override async Task InitializeAsync(CancellationToken aToken)
        {
            await Interface.WriteValueAsync(new byte[] { 0x0 },
                new ButtplugDeviceWriteOptions { Endpoint = Endpoints.Firmware, WriteWithResponse = true },
                aToken).ConfigureAwait(false);
        }

        private Task<ButtplugMessage> HandleStopDeviceCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            // This probably shouldn't be a nop, but right now we don't have a good way to know
            // if the launch is moving or not, and surprisingly enough, setting speed to 0 does not
            // actually stop movement. It just makes it move really slow.
            // However, since each move it makes is finite (unlike setting vibration on some devices),
            // so we can assume it will be a short move, similar to what we do for the Kiiroo toys.
            BpLogger.Debug("Stopping Device " + Name);
            return Task.FromResult<ButtplugMessage>(new Ok(aMsg.Id));
        }

        private async Task<ButtplugMessage> HandleLinearCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckGenericMessageHandler<LinearCmd>(aMsg, 1);
            var v = cmdMsg.Vectors[0];

            return await HandleFleshlightLaunchFW12Cmd(new FleshlightLaunchFW12Cmd(cmdMsg.DeviceIndex,
                Convert.ToUInt32(FleshlightHelper.GetSpeed(Math.Abs(_lastPosition - v.Position), v.Duration) * 99),
                Convert.ToUInt32(v.Position * 99), cmdMsg.Id), aToken).ConfigureAwait(false);
        }

        private async Task<ButtplugMessage> HandleFleshlightLaunchFW12Cmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckMessageHandler<FleshlightLaunchFW12Cmd>(aMsg);

            _lastPosition = Convert.ToDouble(cmdMsg.Position) / 99;

            await Interface.WriteValueAsync(new[] { (byte)cmdMsg.Position, (byte)cmdMsg.Speed },
                aToken).ConfigureAwait(false);
            return new Ok(aMsg.Id);
        }
    }
}
