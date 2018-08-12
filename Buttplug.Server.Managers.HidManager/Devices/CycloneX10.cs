﻿// <copyright file="CycloneX10.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;
using HidLibrary;

namespace Buttplug.Server.Managers.HidManager.Devices
{
    internal class CycloneX10HidDeviceInfo : IHidDeviceInfo
    {
        public string Name { get; } = "Cyclone X10";

        public int VendorId { get; } = 0x0483;

        public int ProductId { get; } = 0x5750;

        public IButtplugDevice CreateDevice(IButtplugLogManager aLogManager, IHidDevice aHid)
        {
            return new CycloneX10(aLogManager, aHid, this);
        }
    }

    internal class CycloneX10 : HidButtplugDevice
    {
        private bool _clockwise = true;

        private double _speed;

        public CycloneX10(IButtplugLogManager aLogManager, IHidDevice aHid, CycloneX10HidDeviceInfo aDeviceInfo)
            : base(aLogManager, aHid, aDeviceInfo)
        {
            MsgFuncs.Add(typeof(VorzeA10CycloneCmd), new ButtplugDeviceMessageHandler(HandleVorzeA10CycloneCmd));
            MsgFuncs.Add(typeof(RotateCmd), new ButtplugDeviceMessageHandler(HandleRotateCmd, new MessageAttributes() { FeatureCount = 1 }));
            MsgFuncs.Add(typeof(StopDeviceCmd), new ButtplugDeviceMessageHandler(HandleStopDeviceCmd));
            aHid.OpenDevice();
        }

        protected override bool HandleData(byte[] data)
        {
            BpLogger.Trace("Cyclone got data: " + BitConverter.ToString(data));
            return true;
        }

        private async Task<ButtplugMessage> HandleStopDeviceCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            BpLogger.Debug("Stopping Device " + Name);
            return await HandleVorzeA10CycloneCmd(new VorzeA10CycloneCmd(aMsg.DeviceIndex, 0, _clockwise, aMsg.Id), aToken);
        }

        private Task<ButtplugMessage> HandleRotateCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            if (!(aMsg is RotateCmd cmdMsg))
            {
                return Task.FromResult<ButtplugMessage>(BpLogger.LogErrorMsg(aMsg.Id, Error.ErrorClass.ERROR_DEVICE, "Wrong Handler"));
            }

            if (cmdMsg.Rotations.Count != 1)
            {
                return Task.FromResult<ButtplugMessage>(new Error(
                    "RotateCmd requires 1 vector for this device.",
                    Error.ErrorClass.ERROR_DEVICE,
                    cmdMsg.Id));
            }

            var changed = false;
            foreach (var i in cmdMsg.Rotations)
            {
                if (i.Index != 0)
                {
                    return Task.FromResult<ButtplugMessage>(new Error(
                        $"Index {i.Index} is out of bounds for RotateCmd for this device.",
                        Error.ErrorClass.ERROR_DEVICE,
                        cmdMsg.Id));
                }

                changed |= _clockwise != i.Clockwise;
                changed |= Math.Abs(_speed - i.Speed) > 0.001;
                _clockwise = i.Clockwise;
                _speed = i.Speed;
            }

            if (!changed)
            {
                return Task.FromResult<ButtplugMessage>(new Ok(cmdMsg.Id));
            }

            // [6] pause 0x30 + 0-1
            // [7] speed 0x30 + 0-10
            // [9] mode  0x30 + 0-9 (0 forwards, 1 backwards, 2+ patterns)
            var data = new byte[] { 0x00, 0x3C, 0x30, 0x31, 0x35, 0x32, 0x30, 0x30, 0x30, 0x30, 0x30, 0x01, 0x02, 0x03, 0x68, 0x3E };

            data[6] += 0;
            data[7] += Convert.ToByte(_clockwise ? 0 : 1);
            data[8] += Convert.ToByte(_speed * 10);

            return WriteData(data) ?
                Task.FromResult<ButtplugMessage>(new Ok(aMsg.Id)) :
                Task.FromResult<ButtplugMessage>(new Error("Failed to send command",
                    Error.ErrorClass.ERROR_DEVICE, aMsg.Id));
        }

        private Task<ButtplugMessage> HandleVorzeA10CycloneCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            if (!(aMsg is VorzeA10CycloneCmd cmdMsg))
            {
                return Task.FromResult<ButtplugMessage>(BpLogger.LogErrorMsg(aMsg.Id, Error.ErrorClass.ERROR_DEVICE, "Wrong Handler"));
            }

            return HandleRotateCmd(new RotateCmd(cmdMsg.DeviceIndex,
                new List<RotateCmd.RotateSubcommand>
                {
                    new RotateCmd.RotateSubcommand(0, Convert.ToDouble(cmdMsg.Speed) / 99, cmdMsg.Clockwise),
                },
                cmdMsg.Id), aToken);
        }
    }
}
