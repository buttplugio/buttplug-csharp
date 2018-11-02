// <copyright file="Youcups.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Devices;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;

namespace Buttplug.Server.Bluetooth.Devices
{
    internal class YoucupsBluetoothInfo : IBluetoothDeviceInfo
    {
        public enum Chrs : uint
        {
            Tx = 0,
        }

        public Guid[] Services { get; } = { new Guid("0000fee9-0000-1000-8000-00805f9b34fb") };

        public string[] NamePrefixes { get; } = { };

        public string[] Names { get; } =
        {
            // Warrior II
            "Youcups",
        };

        public Dictionary<uint, Guid> Characteristics { get; } = new Dictionary<uint, Guid>()
        {
            { (uint)Chrs.Tx, new Guid("d44bc439-abfd-45a2-b575-925416129600") },
        };

        public IButtplugDevice CreateDevice(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
        {
            return new Youcups(aLogManager, aInterface, this);
        }
    }

    internal class Youcups : ButtplugBluetoothDevice
    {
        private static readonly Dictionary<string, string> FriendlyNames = new Dictionary<string, string>
        {
            { "Youcups", "Warrior II" },
        };

        private double _vibratorSpeed;

        public Youcups(IButtplugLogManager aLogManager,
                       IBluetoothDeviceInterface aInterface,
                       IBluetoothDeviceInfo aInfo)
            : base(aLogManager,
                   "Youcups Unknown",
                   aInterface,
                   aInfo)
        {
            if (FriendlyNames.ContainsKey(aInterface.Name))
            {
                Name = $"Youcups {FriendlyNames[aInterface.Name]}";
            }

            AddMessageHandler<SingleMotorVibrateCmd>(HandleSingleMotorVibrateCmd);
            AddMessageHandler<VibrateCmd>(HandleVibrateCmd, new MessageAttributes() { FeatureCount = 1 });
            AddMessageHandler<StopDeviceCmd>(HandleStopDeviceCmd);
        }

        private async Task<ButtplugMessage> HandleStopDeviceCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            BpLogger.Debug("Stopping Device " + Name);
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
            var v = cmdMsg.Speeds[0];

            if (Math.Abs(v.Speed - _vibratorSpeed) < 0.001)
            {
                return new Ok(cmdMsg.Id);
            }

            _vibratorSpeed = v.Speed;

            return await Interface.WriteValueAsync(aMsg.Id,
                Encoding.ASCII.GetBytes($"$SYS,{(int)(_vibratorSpeed * 8),1}?"), false, aToken).ConfigureAwait(false);
        }
    }
}