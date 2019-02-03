// <copyright file="XInputProtocol.cs" company="Nonpolynomial Labs LLC">
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
using JetBrains.Annotations;

namespace Buttplug.Devices.Protocols
{
    public class XInputProtocol : ButtplugDeviceProtocol
    {
        [NotNull]
        private readonly double[] _vibratorSpeeds = { 0, 0 };

        public XInputProtocol(IButtplugLogManager aLogManager, IButtplugDeviceImpl aDevice)
            : base(aLogManager, "XBox Compatible Gamepad (XInput)", aDevice)
        {
            AddMessageHandler<SingleMotorVibrateCmd>(HandleSingleMotorVibrateCmd);
            AddMessageHandler<VibrateCmd>(HandleVibrateCmd, new MessageAttributes() { FeatureCount = 2 });
            AddMessageHandler<StopDeviceCmd>(HandleStopDeviceCmd);
        }

        private Task<ButtplugMessage> HandleStopDeviceCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            BpLogger.Debug("Stopping Device " + Name);
            return HandleSingleMotorVibrateCmd(new SingleMotorVibrateCmd(aMsg.DeviceIndex, 0, aMsg.Id), aToken);
        }

        private Task<ButtplugMessage> HandleSingleMotorVibrateCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckMessageHandler<SingleMotorVibrateCmd>(aMsg);

            var speeds = new List<VibrateCmd.VibrateSubcommand>();
            for (uint i = 0; i < 2; i++)
            {
                speeds.Add(new VibrateCmd.VibrateSubcommand(i, cmdMsg.Speed));
            }

            return HandleVibrateCmd(new VibrateCmd(cmdMsg.DeviceIndex, speeds, cmdMsg.Id), aToken);
        }

        private async Task<ButtplugMessage> HandleVibrateCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckGenericMessageHandler<VibrateCmd>(aMsg, 2);

            foreach (var vi in cmdMsg.Speeds)
            {
                _vibratorSpeeds[vi.Index] = _vibratorSpeeds[vi.Index] < 0 ? 0
                                          : _vibratorSpeeds[vi.Index] > 1 ? 1
                                                                          : vi.Speed;
            }

            // This is gross, but in trying to keep with the "only take bytes in endpoints" rule, we
            // gotta deal with it.
            // todo Possible to optimize this but I currently do not care.
            var speedBytes = new List<byte>();
            speedBytes.AddRange(BitConverter.GetBytes((ushort)(_vibratorSpeeds[0] * ushort.MaxValue)));
            speedBytes.AddRange(BitConverter.GetBytes((ushort)(_vibratorSpeeds[1] * ushort.MaxValue)));

            await Interface.WriteValueAsync(speedBytes.ToArray(), false, aToken);
            return new Ok(aMsg.Id);
        }
    }
}