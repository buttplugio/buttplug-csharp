// <copyright file="VorzeSAProtocol.cs" company="Nonpolynomial Labs LLC">
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

namespace Buttplug.Devices.Protocols
{
    internal class VorzeSAProtocol : ButtplugDeviceProtocol
    {
        private bool _clockwise = true;
        private uint _speed;

        private enum DeviceType
        {
            CycloneOrUnknown = 1,
            UFO = 2,
            Piston = 3,
            Bach = 6,
        }

        public enum CommandType
        {
            Rotate = 1,
            Vibrate = 3,
            Linear = 10,
        }

        public struct VorzePistonCommand
        {
            public uint parentMsgId;
            public LinearCmd.VectorSubcommand cmd;
            public CancellationToken aToken;
        }
        private DateTime _currentTime;
        private DateTime _nextDispatchTime;
        private Queue<VorzePistonCommand> _linearCmdQueue;
        private byte _beforeLinearCmdPosition;

        private DeviceType _deviceType = DeviceType.CycloneOrUnknown;
        private CommandType _commandType = CommandType.Rotate;

        private bool isEnable = false;

        public VorzeSAProtocol(IButtplugLogManager aLogManager,
                       IButtplugDeviceImpl aInterface)
            : base(aLogManager,
                   "Vorze SA Unknown",
                   aInterface)
        {
            switch (aInterface.Name)
            {
                case "CycSA":
                    _deviceType = DeviceType.CycloneOrUnknown;
                    _commandType = CommandType.Rotate;
                    Name = "Vorze A10 Cyclone SA";
                    break;

                case "UFOSA":
                    _deviceType = DeviceType.UFO;
                    _commandType = CommandType.Rotate;
                    Name = "Vorze UFO SA";
                    break;

                case "VorzePiston":
                    _deviceType = DeviceType.Piston;
                    _commandType = CommandType.Linear;
                    Name = "Vorze A10 Piston SA";
                    break;
                case "Bach smart":
                    _deviceType = DeviceType.Bach;
                    _commandType = CommandType.Vibrate;
                    Name = "Vorze Bach";
                    break;

                default:
                    // If the device doesn't identify, warn and try sending it Cyclone packets.
                    BpLogger.Warn($"Vorze product with unrecognized name ({Name}) found. This product may not work with Buttplug. Contact the developers for more info.");
                    break;
            }

            switch (_commandType)
            {
                case CommandType.Rotate:
                    AddMessageHandler<VorzeA10CycloneCmd>(HandleVorzeA10CycloneCmd);
                    AddMessageHandler<RotateCmd>(HandleRotateCmd, new MessageAttributes() { FeatureCount = 1 });
                    break;

                case CommandType.Vibrate:
                    AddMessageHandler<SingleMotorVibrateCmd>(HandleSingleMotorVibrateCmd);
                    AddMessageHandler<VibrateCmd>(HandleVibrateCmd, new MessageAttributes() { FeatureCount = 1 });
                    break;
                case CommandType.Linear:
                    AddMessageHandler<LinearCmd>(HandleLinearCmd, new MessageAttributes() { FeatureCount = 1 });
                    _linearCmdQueue = new Queue<VorzePistonCommand>();
                    break;

                default:
                    BpLogger.Error("Unhandled command type.");
                    break;
            }

            AddMessageHandler<StopDeviceCmd>(HandleStopDeviceCmd);
        }


        private async void ObserveLinearCmdQueue()
        {
            while (isEnable)
            {
                _currentTime = DateTime.Now;
                if (this._linearCmdQueue.Count > 0 && _currentTime >= _nextDispatchTime)
                {
                    var cmd = _linearCmdQueue.Dequeue();
                    await DispatchLinearCmd(cmd.cmd, cmd.aToken, cmd.parentMsgId);
                    _nextDispatchTime = _currentTime.AddMilliseconds(cmd.cmd.Duration);
                }
                await Task.Delay(TimeSpan.FromMilliseconds(10));
            }
        }


        private async Task<ButtplugMessage> HandleStopDeviceCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            BpLogger.Debug("Stopping Device " + Name);

            if (_deviceType == DeviceType.Piston)
            {
                // Forced transition to the front
                await Interface.WriteValueAsync(new byte[] { (byte)_deviceType, 0, 60 }, aToken).ConfigureAwait(false);
            }
            else
            {
                await Interface.WriteValueAsync(new byte[] { (byte)_deviceType, (byte)_commandType, 0 }, aToken).ConfigureAwait(false);
            }
            isEnable = false;

            return new Ok(aMsg.Id);
        }

        private async Task<ButtplugMessage> HandleRotateCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckGenericMessageHandler<RotateCmd>(aMsg, 1);
            var v = cmdMsg.Rotations[0];

            return await HandleVorzeA10CycloneCmd(new VorzeA10CycloneCmd(cmdMsg.DeviceIndex,
                Convert.ToUInt32(v.Speed * 99), v.Clockwise, cmdMsg.Id), aToken).ConfigureAwait(false);
        }

        private async Task<ButtplugMessage> HandleVorzeA10CycloneCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckMessageHandler<VorzeA10CycloneCmd>(aMsg);

            if (_clockwise == cmdMsg.Clockwise && _speed == cmdMsg.Speed && SentRotation)
            {
                return new Ok(cmdMsg.Id);
            }

            SentRotation = true;

            _clockwise = cmdMsg.Clockwise;
            _speed = cmdMsg.Speed;

            var rawSpeed = (byte)((byte)(_clockwise ? 1 : 0) << 7 | (byte)_speed);
            await Interface.WriteValueAsync(new[] { (byte)_deviceType, (byte)_commandType, rawSpeed },
                aToken).ConfigureAwait(false);
            return new Ok(aMsg.Id);
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
                var tmpSpeed = Convert.ToUInt32(v.Speed * 100);
                if (tmpSpeed == _speed)
                {
                    continue;
                }

                changed = true;
                _speed = tmpSpeed;
            }

            if (!changed && SentVibration)
            {
                return new Ok(cmdMsg.Id);
            }

            SentVibration = true;

            await Interface.WriteValueAsync(new[] { (byte)_deviceType, (byte)_commandType, (byte)_speed },
                aToken).ConfigureAwait(false);
            return new Ok(aMsg.Id);
        }


        private async Task<ButtplugMessage> HandleLinearCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            if (!isEnable && _commandType == CommandType.Linear)
            {
                _linearCmdQueue.Clear();
            }

            var cmdMsg = CheckMessageHandler<LinearCmd>(aMsg);
            foreach (var cmd in cmdMsg.Vectors)
            {
                VorzePistonCommand cmdWrapper;
                cmdWrapper.parentMsgId = aMsg.Id;
                cmdWrapper.cmd = cmd;
                cmdWrapper.aToken = aToken;
                _linearCmdQueue.Enqueue(cmdWrapper);
            }

            if (!isEnable && _commandType == CommandType.Linear)
            {
                _currentTime = DateTime.Now;
                _nextDispatchTime = _currentTime;
                isEnable = true;
                await Task.Run(async () => { ObserveLinearCmdQueue(); });
            }

            return new Ok(aMsg.Id);
        }
        private async Task<ButtplugMessage> DispatchLinearCmd(LinearCmd.VectorSubcommand cmd, CancellationToken aToken, uint parentMsgId)
        {
            //byte position = (byte)Math.Ceiling((1d / 200d) * (cmd.Position * 100) * 100);
            byte position = (byte)Math.Ceiling(cmd.Position * 200);
            byte direction = (byte)(_beforeLinearCmdPosition > position ? 0 : 1);
            byte speed = ResolveLinearCmdSpeed(cmd.Duration, direction);

            await Interface.WriteValueAsync(new[] { (byte)_deviceType, position, speed }, aToken).ConfigureAwait(false);
            _beforeLinearCmdPosition = position;

            return new Ok(parentMsgId);
        }

        public byte ResolveLinearCmdSpeed(uint duration, byte direction)
        {
            // Convert back to stroke speed from the interval.
            //
            // Front and back stroke speed is asymmetric.
            // (From back to front is slower than front to back.)
            //
            byte speed = 10; // defaults. minimum.

            if (duration <= 200)
            {
                speed = 60;
            }
            else if (duration <= 300)
            {
                speed = direction > 0 ? (byte)20 : (byte)30;
            }
            else if (duration <= 550)
            {
                speed = direction > 0 ? (byte)15 : (byte)20;
            }
            else if (duration <= 700)
            {
                speed = direction > 0 ? (byte)13 : (byte)20;
            }
            else
            {
                speed = 10;
            }

            return speed;
        }
    }
}
