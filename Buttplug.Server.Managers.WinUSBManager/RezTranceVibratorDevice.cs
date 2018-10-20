// <copyright file="RezTranceVibratorDevice.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Devices;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using MadWizard.WinUSBNet;

namespace Buttplug.Server.Managers.WinUSBManager
{
    internal class RezTranceVibratorDevice : ButtplugDevice
    {
        private static uint _vibratorCount = 1;
        private USBDevice _device;

        public RezTranceVibratorDevice(IButtplugLogManager aLogManager, USBDevice aDevice, uint aIndex)
            : base(aLogManager, "Trancevibrator " + aIndex, "Trancevibrator " + aIndex)
        {
            _device = aDevice;
            AddMessageHandler<SingleMotorVibrateCmd>(HandleSingleMotorVibrateCmd);
            AddMessageHandler<StopDeviceCmd>(HandleStopDeviceCmd);
            AddMessageHandler<VibrateCmd>(HandleVibrateCmd, new MessageAttributes { FeatureCount = 1 });
        }

        public override void Disconnect()
        {
        }

        private Task<ButtplugMessage> HandleStopDeviceCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            BpLogger.Debug("Stopping Device " + Name);
            return HandleSingleMotorVibrateCmd(new SingleMotorVibrateCmd(aMsg.DeviceIndex, 0, aMsg.Id), aToken);
        }

        private Task<ButtplugMessage> HandleVibrateCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckMessageHandler<VibrateCmd>(aMsg);

            if (cmdMsg.Speeds.Count == 0 || cmdMsg.Speeds.Count > _vibratorCount)
            {
                throw new ButtplugDeviceException(BpLogger, "VibrateCmd requires 1 speed for this device.",
                    cmdMsg.Id);
            }

            foreach (var v in cmdMsg.Speeds)
            {
                if (v.Index >= _vibratorCount)
                {
                    throw new ButtplugDeviceException(BpLogger,
                        $"Index {v.Index} is out of bounds for VibrateCmd for this device.",
                        cmdMsg.Id);
                }

                var speed = (byte)Math.Floor(v.Speed * 255);
                _device.ControlOut(
                    0x02 << 5 | // Vendor Type
                    0x01 | // Interface Recipient
                    0x00, // Out Enpoint
                    1,
                    speed,
                    0);
            }

            return Task.FromResult<ButtplugMessage>(new Ok(aMsg.Id));
        }

        private Task<ButtplugMessage> HandleSingleMotorVibrateCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckMessageHandler<SingleMotorVibrateCmd>(aMsg);

            var speeds = new List<VibrateCmd.VibrateSubcommand>();
            for (uint i = 0; i < _vibratorCount; i++)
            {
                speeds.Add(new VibrateCmd.VibrateSubcommand(i, cmdMsg.Speed));
            }

            return HandleVibrateCmd(new VibrateCmd(aMsg.DeviceIndex, speeds, aMsg.Id), aToken);
        }
    }
}
