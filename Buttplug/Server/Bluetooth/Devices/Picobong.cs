// <copyright file="Picobong.cs" company="Nonpolynomial Labs LLC">
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
    internal class PicobongBluetoothInfo : IBluetoothDeviceInfo
    {
        public enum Chrs : uint
        {
            Tx = 0,
            Rx,
        }

        public Guid[] Services { get; } = { new Guid("0000fff0-0000-1000-8000-00805f9b34fb") };

        public string[] NamePrefixes { get; } = { };

        public string[] Names { get; } =
        {
            "Blow hole",
            "Picobong Male Toy",
            "Diver",
            "Picobong Egg",
            "Life guard",
            "Picobong Ring",
            "Surfer",
            "Picobong Butt Plug",
            "Egg driver",
            "Surfer_plug",
        };

        // WeVibe causes the characteristic detector to misidentify characteristics. Do not remove these.
        public Dictionary<uint, Guid> Characteristics { get; } = new Dictionary<uint, Guid>()
        {
            // tx characteristic
            { (uint)Chrs.Tx, new Guid("0000fff1-0000-1000-8000-00805f9b34fb") },
        };

        public IButtplugDevice CreateDevice(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
        {
            return new Picobong(aLogManager, aInterface, this);
        }
    }

    internal class Picobong : ButtplugBluetoothDevice
    {
        private static readonly Dictionary<string,string> NameMap = new Dictionary<string, string>()
        {
            { "Blow hole", "Blow hole" },
            { "Picobong Male Toy", "Blow hole" },
            { "Diver", "Diver" },
            { "Picobong Egg", "Diver" },
            { "Life guard", "Life guard" },
            { "Picobong Ring", "Life guard" },
            { "Surfer", "Surfer"},
            { "Picobong Butt Plug", "Surfer" },
            { "Egg driver", "Surfer" },
            { "Surfer_plug", "Surfer" },
        };

        private double _vibratorSpeed = 0;

        public Picobong(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface,
            IBluetoothDeviceInfo aInfo)
            : base(aLogManager,
                $"Picobong {aInterface.Name}",
                aInterface,
                aInfo)
        {
            if (NameMap.ContainsKey(Name))
            {
                Name = NameMap[Name];
            }

            AddMessageHandler<SingleMotorVibrateCmd>(HandleSingleMotorVibrateCmd);
            AddMessageHandler<VibrateCmd>(HandleVibrateCmd, new MessageAttributes() { FeatureCount = 1 });
            AddMessageHandler<StopDeviceCmd>(HandleStopDeviceCmd);
        }

        private async Task<ButtplugMessage> HandleStopDeviceCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            return await HandleSingleMotorVibrateCmd(new SingleMotorVibrateCmd(aMsg.DeviceIndex, 0, aMsg.Id), aToken).ConfigureAwait(false);
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
                if (!(Math.Abs(v.Speed - _vibratorSpeed) > 0.001))
                {
                    continue;
                }

                changed = true;
                _vibratorSpeed = v.Speed;
            }

            if (!changed)
            {
                return new Ok(cmdMsg.Id);
            }

            var speedInt = Convert.ToUInt16(_vibratorSpeed * 10);
            
            var data = new byte[] { 0x01, speedInt > 0 ? (byte)0x01 : (byte)0xff, Convert.ToByte(speedInt) };

            return await Interface.WriteValueAsync(aMsg.Id, (uint)PicobongBluetoothInfo.Chrs.Tx, data, false, aToken).ConfigureAwait(false);
        }
    }
}