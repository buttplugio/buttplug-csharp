// <copyright file="WeVibe.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Devices;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;

namespace Buttplug.Server.Bluetooth.Devices
{
    internal class WeVibeBluetoothInfo : IBluetoothDeviceInfo
    {
        public enum Chrs : uint
        {
            Tx = 0,
            Rx,
        }

        public Guid[] Services { get; } = { new Guid("f000bb03-0451-4000-b000-000000000000") };

        public string[] NamePrefixes { get; } = { };

        public string[] Names { get; } =
        {
            "Cougar",
            "4 Plus",
            "4plus",
            "Bloom",
            "classic",
            "Classic",
            "Ditto",
            "Gala",
            "Jive",
            "Nova",
            "NOVAV2",
            "Pivot",
            "Rave",
            "Sync",
            "Verge",
            "Wish",
        };

        // WeVibe causes the characteristic detector to misidentify characteristics. Do not remove these.
        public Dictionary<uint, Guid> Characteristics { get; } = new Dictionary<uint, Guid>()
        {
            // tx characteristic
            { (uint)Chrs.Tx, new Guid("f000c000-0451-4000-b000-000000000000") },
            { (uint)Chrs.Rx, new Guid("f000b000-0451-4000-b000-000000000000") },
        };

        public IButtplugDevice CreateDevice(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
        {
            return new WeVibe(aLogManager, aInterface, this);
        }
    }

    internal class WeVibe : ButtplugBluetoothDevice
    {
        private static readonly string[] DualVibes =
        {
            "Cougar",
            "4 Plus",
            "4plus",
            "classic",
            "Classic",
            "Gala",
            "Nova",
            "NOVAV2",
            "Sync",
        };

        private readonly uint _vibratorCount = 1;
        private readonly double[] _vibratorSpeed = { 0, 0 };

        public WeVibe(IButtplugLogManager aLogManager,
                      IBluetoothDeviceInterface aInterface,
                      IBluetoothDeviceInfo aInfo)
            : base(aLogManager,
                   $"WeVibe {aInterface.Name}",
                   aInterface,
                   aInfo)
        {
            if (DualVibes.Contains(aInterface.Name))
            {
                _vibratorCount = 2;
            }

            AddMessageHandler<SingleMotorVibrateCmd>(HandleSingleMotorVibrateCmd);
            AddMessageHandler<VibrateCmd>(HandleVibrateCmd, new MessageAttributes() { FeatureCount = _vibratorCount });
            AddMessageHandler<StopDeviceCmd>(HandleStopDeviceCmd);
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

            var rSpeedInt = Convert.ToUInt16(_vibratorSpeed[0] * 15);
            var rSpeedExt = Convert.ToUInt16(_vibratorSpeed[_vibratorCount - 1] * 15);

            // 0f 03 00 bc 00 00 00 00
            var data = new byte[] { 0x0f, 0x03, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00 };
            data[3] = Convert.ToByte(rSpeedExt); // External
            data[3] |= Convert.ToByte(rSpeedInt << 4); // Internal

            // ReSharper disable once InvertIf
            if (rSpeedInt == 0 && rSpeedExt == 0)
            {
                data[1] = 0x00;
                data[5] = 0x00;
            }

            return await Interface.WriteValueAsync(aMsg.Id, (uint)WeVibeBluetoothInfo.Chrs.Tx, data, false, aToken).ConfigureAwait(false);
        }
    }
}