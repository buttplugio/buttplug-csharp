// <copyright file="VorzeSA.cs" company="Nonpolynomial Labs LLC">
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

namespace Buttplug.Server.Bluetooth.Devices
{
    internal class VorzeSABluetoothInfo : IBluetoothDeviceInfo
    {
        public enum Chrs : uint
        {
            Tx = 0,
        }

        public Guid[] Services { get; } = { new Guid("40ee1111-63ec-4b7f-8ce7-712efd55b90e") };

        public string[] Names { get; } = { "CycSA", "UFOSA" };

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
        }

        private DeviceType _deviceType = DeviceType.CycloneOrUnknown;

        public VorzeSA(IButtplugLogManager aLogManager,
                       IBluetoothDeviceInterface aInterface,
                       IBluetoothDeviceInfo aInfo)
            : base(aLogManager,
                   "Vorze SA Unknown",
                   aInterface,
                   aInfo)
        {
            if (aInterface.Name == "CycSA")
            {
                _deviceType = DeviceType.CycloneOrUnknown;
                Name = "Vorze A10 Cyclone SA";
            }
            else if (aInterface.Name == "UFOSA")
            {
                _deviceType = DeviceType.UFO;
                Name = "Vorze UFO SA";
            }
            else
            {
                // If the device doesn't identify, warn and try sending it Cyclone packets.
                BpLogger.Warn($"Vorze product with unrecognized name ({Name}) found. This product may not work with Buttplug. Contact the developers for more info.");
            }

            MsgFuncs.Add(typeof(VorzeA10CycloneCmd), new ButtplugDeviceMessageHandler(HandleVorzeA10CycloneCmd));
            MsgFuncs.Add(typeof(RotateCmd), new ButtplugDeviceMessageHandler(HandleRotateCmd, new MessageAttributes() { FeatureCount = 1 }));
            MsgFuncs.Add(typeof(StopDeviceCmd), new ButtplugDeviceMessageHandler(HandleStopDeviceCmd));
        }

        private async Task<ButtplugMessage> HandleStopDeviceCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            BpLogger.Debug("Stopping Device " + Name);
            return await HandleVorzeA10CycloneCmd(new VorzeA10CycloneCmd(aMsg.DeviceIndex, 0, _clockwise, aMsg.Id), aToken);
        }

        private async Task<ButtplugMessage> HandleRotateCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            if (!(aMsg is RotateCmd cmdMsg))
            {
                return BpLogger.LogErrorMsg(aMsg.Id, Error.ErrorClass.ERROR_DEVICE, "Wrong Handler");
            }

            if (cmdMsg.Rotations.Count != 1)
            {
                return new Error(
                    "RotateCmd requires 1 vector for this device.",
                    Error.ErrorClass.ERROR_DEVICE,
                    cmdMsg.Id);
            }

            foreach (var i in cmdMsg.Rotations)
            {
                if (i.Index != 0)
                {
                    return new Error(
                        $"Index {i.Index} is out of bounds for RotateCmd for this device.",
                        Error.ErrorClass.ERROR_DEVICE,
                        cmdMsg.Id);
                }

                return await HandleVorzeA10CycloneCmd(new VorzeA10CycloneCmd(cmdMsg.DeviceIndex,
                    Convert.ToUInt32(i.Speed * 99), i.Clockwise, cmdMsg.Id), aToken);
            }

            return new Ok(cmdMsg.Id);
        }

        private async Task<ButtplugMessage> HandleVorzeA10CycloneCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            if (!(aMsg is VorzeA10CycloneCmd cmdMsg))
            {
                return BpLogger.LogErrorMsg(aMsg.Id, Error.ErrorClass.ERROR_DEVICE, "Wrong Handler");
            }

            if (_clockwise == cmdMsg.Clockwise && _speed == cmdMsg.Speed)
            {
                return new Ok(cmdMsg.Id);
            }

            _clockwise = cmdMsg.Clockwise;
            _speed = cmdMsg.Speed;

            var rawSpeed = (byte)((byte)(_clockwise ? 1 : 0) << 7 | (byte)_speed);
            return await Interface.WriteValue(aMsg.Id,
                (uint)VorzeSABluetoothInfo.Chrs.Tx,
                new byte[] { (byte)_deviceType, 0x01, rawSpeed }, false, aToken);
        }
    }
}