// <copyright file="Vibratissimo.cs" company="Nonpolynomial Labs LLC">
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
    internal class VibratissimoBluetoothInfo : IBluetoothDeviceInfo
    {
        public enum Chrs : uint
        {
            TxMode = 0,
            TxSpeed,
            Rx,
        }

        public Guid[] Services { get; } = { new Guid("00001523-1212-efde-1523-785feabcd123") };

        // Device can be renamed, but using wildcard names spams our logs and they
        // reuse a common Service UUID, so require it to be the default
        public string[] Names { get; } =
        {
            "Vibratissimo",
        };

        public Dictionary<uint, Guid> Characteristics { get; } = new Dictionary<uint, Guid>()
        {
            { (uint)Chrs.TxMode, new Guid("00001524-1212-efde-1523-785feabcd123") },
            { (uint)Chrs.TxSpeed, new Guid("00001526-1212-efde-1523-785feabcd123") },
            { (uint)Chrs.Rx, new Guid("00001527-1212-efde-1523-785feabcd123") },
        };

        public string[] NamePrefixes { get; } = { };

        public IButtplugDevice CreateDevice(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
        {
            return new Vibratissimo(aLogManager, aInterface, this);
        }
    }

    internal class Vibratissimo : ButtplugBluetoothDevice
    {
        private double _vibratorSpeed;

        public Vibratissimo(IButtplugLogManager aLogManager,
                            IBluetoothDeviceInterface aInterface,
                            IBluetoothDeviceInfo aInfo)
            : base(aLogManager,
                   $"Vibratissimo Device ({aInterface.Name})",
                   aInterface,
                   aInfo)
        {
            AddMessageHandler<SingleMotorVibrateCmd>(HandleSingleMotorVibrateCmd);
            AddMessageHandler<VibrateCmd>(HandleVibrateCmd, new MessageAttributes { FeatureCount = 1 });
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

            var data = new byte[] { 0x03, 0xff };
            await Interface.WriteValueAsync(aMsg.Id,
                (uint)VibratissimoBluetoothInfo.Chrs.TxMode,
                data, false, aToken).ConfigureAwait(false);

            data[0] = Convert.ToByte(_vibratorSpeed * byte.MaxValue);
            data[1] = 0x00;
            return await Interface.WriteValueAsync(aMsg.Id,
                (uint)VibratissimoBluetoothInfo.Chrs.TxSpeed,
                data, false, aToken).ConfigureAwait(false);
        }
    }
}
