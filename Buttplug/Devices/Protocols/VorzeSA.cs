// <copyright file="VorzeSA.cs" company="Nonpolynomial Labs LLC">
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

namespace Buttplug.Server.Bluetooth.Devices
{
    internal class VorzeSABluetoothInfo : IBluetoothDeviceInfo
    {
        public enum Chrs : uint
        {
            Tx = 0,
        }

        public Guid[] Services { get; } = { new Guid("40ee1111-63ec-4b7f-8ce7-712efd55b90e") };

        public string[] Names { get; } = { "CycSA", "UFOSA", "Bach smart" };

        public Dictionary<uint, Guid> Characteristics { get; } = new Dictionary<uint, Guid>()
        {
            { (uint)Chrs.Tx, new Guid("40ee2222-63ec-4b7f-8ce7-712efd55b90e") },
        };

        public string[] NamePrefixes { get; } = { };

        public IButtplugDevice CreateDevice(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
        {
            return new VorzeSA(aLogManager, aInterface, this);
        }
    }

    internal class VorzeSA : ButtplugBluetoothDevice
    {
        private bool _clockwise = true;
        private uint _speed;

        private enum DeviceType
        {
            CycloneOrUnknown = 1,
            UFO = 2,
            Bach = 6,
        }
        public enum CommandType
        {
            Rotate = 1,
            Vibrate = 3,
        }

        private DeviceType _deviceType = DeviceType.CycloneOrUnknown;
        private CommandType _commandType = CommandType.Rotate;

        public VorzeSA(IButtplugLogManager aLogManager,
                       IBluetoothDeviceInterface aInterface,
                       IBluetoothDeviceInfo aInfo)
            : base(aLogManager,
                   "Vorze SA Unknown",
                   aInterface,
                   aInfo)
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

                default:
                    BpLogger.Error("Unhandled command type.");
                    break;
            }

            AddMessageHandler<StopDeviceCmd>(HandleStopDeviceCmd);
        }

        private async Task<ButtplugMessage> HandleStopDeviceCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            BpLogger.Debug("Stopping Device " + Name);
            if (_speed == 0)
            {
                return new Ok(aMsg.Id);
            }

            return await Interface.WriteValueAsync(aMsg.Id,
                (uint)VorzeSABluetoothInfo.Chrs.Tx,
                new byte[] { (byte)_deviceType, (byte)_commandType, 0 }, false, aToken).ConfigureAwait(false);
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

            if (_clockwise == cmdMsg.Clockwise && _speed == cmdMsg.Speed)
            {
                return new Ok(cmdMsg.Id);
            }

            _clockwise = cmdMsg.Clockwise;
            _speed = cmdMsg.Speed;

            var rawSpeed = (byte)((byte)(_clockwise ? 1 : 0) << 7 | (byte)_speed);
            return await Interface.WriteValueAsync(aMsg.Id,
                (uint)VorzeSABluetoothInfo.Chrs.Tx,
                new byte[] { (byte)_deviceType, (byte)_commandType, rawSpeed }, false, aToken).ConfigureAwait(false);
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

            if (!changed)
            {
                return new Ok(cmdMsg.Id);
            }

            return await Interface.WriteValueAsync(aMsg.Id,
                (uint)VorzeSABluetoothInfo.Chrs.Tx,
                new[] { (byte)_deviceType, (byte)_commandType, (byte)_speed }, false, aToken).ConfigureAwait(false);
        }
    }
}