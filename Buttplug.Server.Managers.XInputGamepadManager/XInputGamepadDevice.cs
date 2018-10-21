// <copyright file="XInputGamepadDevice.cs" company="Nonpolynomial Labs LLC">
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
using SharpDX.XInput;

namespace Buttplug.Server.Managers.XInputGamepadManager
{
    internal class XInputGamepadDevice : ButtplugDevice
    {
        private Controller _device;
        private double[] _vibratorSpeeds = { 0, 0 };

        public XInputGamepadDevice(IButtplugLogManager aLogManager, Controller aDevice)
            : base(aLogManager, "XBox Compatible Gamepad (XInput)", aDevice.UserIndex.ToString())
        {
            _device = aDevice;
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

        private Task<ButtplugMessage> HandleVibrateCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckGenericMessageHandler<VibrateCmd>(aMsg, 2);

            foreach (var vi in cmdMsg.Speeds)
            {
                _vibratorSpeeds[vi.Index] = _vibratorSpeeds[vi.Index] < 0 ? 0
                                          : _vibratorSpeeds[vi.Index] > 1 ? 1
                                                                          : vi.Speed;
            }

            var v = new Vibration
            {
                LeftMotorSpeed = (ushort)(_vibratorSpeeds[0] * ushort.MaxValue),
                RightMotorSpeed = (ushort)(_vibratorSpeeds[1] * ushort.MaxValue),
            };

            try
            {
                _device?.SetVibration(v);
            }
            catch (Exception e)
            {
                if (_device?.IsConnected != true)
                {
                    InvokeDeviceRemoved();

                    // Don't throw a spanner in the works
                    return Task.FromResult<ButtplugMessage>(new Ok(aMsg.Id));
                }

                throw new ButtplugDeviceException(BpLogger, e.Message, aMsg.Id);
            }

            return Task.FromResult<ButtplugMessage>(new Ok(aMsg.Id));
        }

        public override void Disconnect()
        {
            var v = new Vibration
            {
                LeftMotorSpeed = 0,
                RightMotorSpeed = 0,
            };
            try
            {
                _device?.SetVibration(v);
                _device = null;
            }
            finally
            {
                _device = null;
            }
        }
    }
}
