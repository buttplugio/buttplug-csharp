// <copyright file="TestDevice.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

// Test file, disable ConfigureAwait checking.
// ReSharper disable ConsiderUsingConfigureAwait

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using Buttplug.Devices;

namespace Buttplug.Test
{
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Test classes can skip documentation requirements")]
    public class TestProtocol : ButtplugDeviceProtocol
    {
        public double V1;
        public double V2;

        public TestProtocol(IButtplugLogManager aLogManager,
            IButtplugDeviceImpl aInterface)
            : base(aLogManager, "Test Device", aInterface)
        {
            AddMessageHandler<SingleMotorVibrateCmd>(HandleSingleMotorVibrateCmd);
            AddMessageHandler<VibrateCmd>(HandleVibrateCmd, new MessageAttributes() { FeatureCount = 2 });
            AddMessageHandler<StopDeviceCmd>(HandleStopDeviceCmd);
        }

        private Task<ButtplugMessage> HandleStopDeviceCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            V1 = V2 = 0;
            return Task.FromResult<ButtplugMessage>(new Ok(aMsg.Id));
        }

        private Task<ButtplugMessage> HandleVibrateCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckMessageHandler<VibrateCmd>(aMsg);

            if (cmdMsg.Speeds.Count < 1 || cmdMsg.Speeds.Count > 2)
            {
                throw new ButtplugDeviceException(
                    "VibrateCmd requires between 1 and 2 vectors for this device.",
                    cmdMsg.Id);
            }

            foreach (var vi in cmdMsg.Speeds)
            {
                if (vi.Index == 0)
                {
                    V1 = vi.Speed;
                }
                else if (vi.Index == 1)
                {
                    V2 = vi.Speed;
                }
                else
                {
                    throw new ButtplugDeviceException(BpLogger,
                        $"Index {vi.Index} is out of bounds for VibrateCmd for this device.",
                        cmdMsg.Id);
                }
            }

            return Task.FromResult<ButtplugMessage>(new Ok(aMsg.Id));
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
    }
}
