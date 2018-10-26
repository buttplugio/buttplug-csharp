// <copyright file="FleshlightLaunch.cs" company="Nonpolynomial Labs LLC">
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
using Buttplug.Server.Util;
using JetBrains.Annotations;

namespace Buttplug.Server.Bluetooth.Devices
{
    internal class FleshlightLaunchBluetoothInfo : IBluetoothDeviceInfo
    {
        public enum Chrs : uint
        {
            Tx = 0,
            Rx = 1,
            Cmd = 2,
        }

        public Dictionary<uint, Guid> Characteristics { get; } = new Dictionary<uint, Guid>()
        {
            // tx
            { (uint)Chrs.Tx, new Guid("88f80581-0000-01e6-aace-0002a5d5c51b") },

            // rx
            { (uint)Chrs.Rx, new Guid("88f80582-0000-01e6-aace-0002a5d5c51b") },

            // cmd
            { (uint)Chrs.Cmd, new Guid("88f80583-0000-01e6-aace-0002a5d5c51b") },
        };

        public Guid[] Services { get; } = { new Guid("88f80580-0000-01e6-aace-0002a5d5c51b") };

        public string[] Names { get; } = { "Launch" };

        public string[] NamePrefixes { get; } = { };

        public IButtplugDevice CreateDevice(
            IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
        {
            return new FleshlightLaunch(aLogManager, aInterface, this);
        }
    }

    internal class KiirooOnyx2BluetoothInfo : IBluetoothDeviceInfo
    {
        public enum Chrs : uint
        {
            Tx = 0,
            Rx,
            Cmd,
        }

        public string[] Names { get; } = { "Onyx2" };

        public string[] NamePrefixes { get; } = { };

        public Guid[] Services { get; } = { new Guid("f60402a6-0293-4bdb-9f20-6758133f7090") };

        public Dictionary<uint, Guid> Characteristics { get; } = new Dictionary<uint, Guid>
        {
            // Tx
            { (uint)Chrs.Tx, new Guid("02962ac9-e86f-4094-989d-231d69995fc2") },

            // Rx
            { (uint)Chrs.Rx, new Guid("d44d0393-0731-43b3-a373-8fc70b1f3323") },

            // Cmd
            { (uint)Chrs.Cmd, new Guid("c7b7a04b-2cc4-40ff-8b10-5d531d1161db") },
        };

        public IButtplugDevice CreateDevice(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
        {
            return new FleshlightLaunch(aLogManager, aInterface, this);
        }
    }

    internal class FleshlightLaunch : ButtplugBluetoothDevice
    {
        private static Dictionary<string, string> _brandNames = new Dictionary<string, string>
        {
            { "Launch", "Fleshlight" },
            { "Onyx2", "Kiiroo" },
        };

        private double _lastPosition;

        public FleshlightLaunch([NotNull] IButtplugLogManager aLogManager,
                                [NotNull] IBluetoothDeviceInterface aInterface,
                                [NotNull] IBluetoothDeviceInfo aInfo)
            : base(aLogManager,
                   aInterface.Name,
                   aInterface,
                   aInfo)
        {
            if (_brandNames.ContainsKey(aInterface.Name))
            {
                Name = $"{_brandNames[aInterface.Name]} {aInterface.Name}";
            }

            // Setup message function array
            AddMessageHandler<FleshlightLaunchFW12Cmd>(HandleFleshlightLaunchFW12Cmd);
            AddMessageHandler<LinearCmd>(HandleLinearCmd, new MessageAttributes() { FeatureCount = 1 });
            AddMessageHandler<StopDeviceCmd>(HandleStopDeviceCmd);
        }

        public override async Task<ButtplugMessage> InitializeAsync(CancellationToken aToken)
        {
            BpLogger.Trace($"Initializing {Name}");
            var chr = (uint)FleshlightLaunchBluetoothInfo.Chrs.Cmd;

            if (Name == "Kiiroo Onyx2")
            {
                chr = (uint)KiirooOnyx2BluetoothInfo.Chrs.Cmd;
            }

            return await Interface.WriteValueAsync(ButtplugConsts.SystemMsgId,
                chr,
                new byte[] { 0 },
                true, aToken).ConfigureAwait(false);
        }

        private Task<ButtplugMessage> HandleStopDeviceCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            // This probably shouldn't be a nop, but right now we don't have a good way to know
            // if the launch is moving or not, and surprisingly enough, setting speed to 0 does not
            // actually stop movement. It just makes it move really slow.
            // However, since each move it makes is finite (unlike setting vibration on some devices),
            // so we can assume it will be a short move, similar to what we do for the Kiiroo toys.
            BpLogger.Debug("Stopping Device " + Name);
            return Task.FromResult<ButtplugMessage>(new Ok(aMsg.Id));
        }

        private async Task<ButtplugMessage> HandleLinearCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckGenericMessageHandler<LinearCmd>(aMsg, 1);
            var v = cmdMsg.Vectors[0];

            return await HandleFleshlightLaunchFW12Cmd(new FleshlightLaunchFW12Cmd(cmdMsg.DeviceIndex,
                Convert.ToUInt32(FleshlightHelper.GetSpeed(Math.Abs(_lastPosition - v.Position), v.Duration) * 99),
                Convert.ToUInt32(v.Position * 99), cmdMsg.Id), aToken).ConfigureAwait(false);
        }

        private async Task<ButtplugMessage> HandleFleshlightLaunchFW12Cmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckMessageHandler<FleshlightLaunchFW12Cmd>(aMsg);

            _lastPosition = Convert.ToDouble(cmdMsg.Position) / 99;

            return await Interface.WriteValueAsync(aMsg.Id,
                (int)FleshlightLaunchBluetoothInfo.Chrs.Tx,
                new[] { (byte)cmdMsg.Position, (byte)cmdMsg.Speed }, false, aToken).ConfigureAwait(false);
        }
    }
}
