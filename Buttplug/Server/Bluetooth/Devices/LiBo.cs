// <copyright file="LiBo.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Devices;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;

namespace Buttplug.Server.Bluetooth.Devices
{
    internal class LiBoBluetoothInfo : IBluetoothDeviceInfo
    {
        public enum Chrs : uint
        {
            WriteShock = 0,
            WriteVibrate,
            ReadBattery,
        }

        public Guid[] Services { get; } =
        {
            new Guid("00006000-0000-1000-8000-00805f9b34fb"), // Write Service

            // TODO Commenting out battery service until we can handle multiple services.

            // new Guid("00006050-0000-1000-8000-00805f9b34fb"), // Read service (battery)
        };

        public string[] NamePrefixes { get; } = { };

        public string[] Names { get; } =
        {
            "PiPiJing",
        };

        public Dictionary<uint, Guid> Characteristics { get; } = new Dictionary<uint, Guid>()
        {
            // tx1 characteristic
            { (uint)Chrs.WriteShock, new Guid("00006001-0000-1000-8000-00805f9b34fb") }, // Shock

            // tx2 characteristic
            { (uint)Chrs.WriteVibrate, new Guid("00006002-0000-1000-8000-00805f9b34fb") }, // VibeMode
            /*
            // rx characteristic
            { (uint)Chrs.ReadBattery,  new Guid("00006051-0000-1000-8000-00805f9b34fb") }, // Read for battery level
            */
        };

        public IButtplugDevice CreateDevice(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
        {
            return new LiBo(aLogManager, aInterface, this);
        }
    }

    internal class LiBo : ButtplugBluetoothDevice
    {
        private readonly uint _vibratorCount = 1;
        private readonly double[] _vibratorSpeed = { 0 };

        public LiBo(IButtplugLogManager aLogManager,
                      IBluetoothDeviceInterface aInterface,
                      IBluetoothDeviceInfo aInfo)
            : base(aLogManager,
                   $"LiBo ({aInterface.Name})",
                   aInterface,
                   aInfo)
        {
            AddMessageHandler<SingleMotorVibrateCmd>(HandleSingleMotorVibrateCmd);
            AddMessageHandler<VibrateCmd>(HandleVibrateCmd, new MessageAttributes() { FeatureCount = _vibratorCount });
            AddMessageHandler<StopDeviceCmd>(HandleStopDeviceCmd);

            // TODO Add a handler for Estim shocking, add a battery handler.
        }

        private async Task<ButtplugMessage> HandleStopDeviceCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            return await HandleSingleMotorVibrateCmd(new SingleMotorVibrateCmd(aMsg.DeviceIndex, 0, aMsg.Id), aToken).ConfigureAwait(false);
        }

        private async Task<ButtplugMessage> HandleSingleMotorVibrateCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckMessageHandler<SingleMotorVibrateCmd>(aMsg);

            return await HandleVibrateCmd(VibrateCmd.Create(cmdMsg.DeviceIndex, cmdMsg.Id, cmdMsg.Speed, _vibratorCount), aToken).ConfigureAwait(false);
        }

        private async Task<ButtplugMessage> HandleVibrateCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckGenericMessageHandler<VibrateCmd>(aMsg, _vibratorCount);

            var changed = false;
            foreach (var v in cmdMsg.Speeds)
            {
                if (!(Math.Abs(v.Speed - _vibratorSpeed[v.Index]) > 0.001))
                {
                    continue;
                }

                changed = true;
                _vibratorSpeed[v.Index] = v.Speed;
            }

            if (!changed)
            {
                return new Ok(cmdMsg.Id);
            }

            // Map a 0 - 100% value to a 0 - 3 value since 0 * x == 0 this will turn off the vibe if
            // speed is 0.00
            var mode = (int)Math.Ceiling(_vibratorSpeed[0] * 3);

            var data = new[] { Convert.ToByte(mode) };

            return await Interface.WriteValueAsync(aMsg.Id,
                (uint)LiBoBluetoothInfo.Chrs.WriteVibrate,
                data, false, aToken).ConfigureAwait(false);
        }
    }
}