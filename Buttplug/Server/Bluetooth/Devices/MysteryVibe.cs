// <copyright file="MysteryVibe.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Buttplug.Core;
using Buttplug.Core.Devices;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;

namespace Buttplug.Server.Bluetooth.Devices
{
    internal class MysteryVibeBluetoothInfo : IBluetoothDeviceInfo
    {
        public enum Chrs : uint
        {
            ModeControl = 0,
            MotorControl = 1,
        }

        public Guid[] Services { get; } = { new Guid("f0006900-110c-478b-b74b-6f403b364a9c") };

        public Dictionary<uint, Guid> Characteristics { get; } = new Dictionary<uint, Guid>()
        {
            { (uint)Chrs.ModeControl, new Guid("f0006901-110c-478B-B74B-6F403B364A9C") },
            { (uint)Chrs.MotorControl, new Guid("f0006903-110c-478B-B74B-6F403B364A9C") },
        };

        public string[] NamePrefixes { get; } = { };

        public string[] Names { get; } =
        {
            "MV Crescendo",
        };

        public IButtplugDevice CreateDevice(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
        {
            return new MysteryVibe(aLogManager, aInterface, this);
        }
    }

    internal class MysteryVibe : ButtplugBluetoothDevice
    {
        internal static readonly byte[] NullSpeed = { 0, 0, 0, 0, 0, 0 };

        // This max speed seems weird, but going over it causes the device to slow down?
        internal static readonly byte MaxSpeed = 56;

        // Approximate timing delay taken from watching packet timing and testing manually.
        internal static readonly uint DelayTimeMS = 93;

        private readonly System.Timers.Timer _updateValueTimer = new System.Timers.Timer();
        private byte[] _vibratorSpeeds = NullSpeed;
        private CancellationTokenSource _stopUpdateCommandSource = new CancellationTokenSource();

        public MysteryVibe(IButtplugLogManager aLogManager,
                       IBluetoothDeviceInterface aInterface,
                       IBluetoothDeviceInfo aInfo)
            : base(aLogManager,
                   "MysteryVibe Crescendo",
                   aInterface,
                   aInfo)
        {
            // Create a new timer that wont fire any events just yet
            _updateValueTimer.Interval = DelayTimeMS;
            _updateValueTimer.Elapsed += MysteryVibeUpdateHandler;
            _updateValueTimer.Enabled = false;
            aInterface.DeviceRemoved += OnDeviceRemoved;

            AddMessageHandler<SingleMotorVibrateCmd>(HandleSingleMotorVibrateCmd);
            AddMessageHandler<VibrateCmd>(HandleVibrateCmd, new MessageAttributes { FeatureCount = 6 });
            AddMessageHandler<StopDeviceCmd>(HandleStopDeviceCmd);
        }

        public override async Task<ButtplugMessage> InitializeAsync(CancellationToken aToken)
        {
            BpLogger.Trace($"Initializing {Name}");

            // Kick Vibrator into motor control mode, just copying what the app sends when you go to
            // create pattern mode.
            return await Interface.WriteValueAsync(ButtplugConsts.SystemMsgId,
                (uint)MysteryVibeBluetoothInfo.Chrs.ModeControl,
                new byte[] { 0x43, 0x02, 0x00 }, true, aToken).ConfigureAwait(false);
        }

        private void OnDeviceRemoved(object aEvent, EventArgs aArgs)
        {
            // Timer should be turned off on removal.
            _updateValueTimer.Enabled = false;

            // Clean up event handler for that magic day when devices manage to disconnect.
            Interface.DeviceRemoved -= OnDeviceRemoved;
        }

        private async void MysteryVibeUpdateHandler(object aEvent, ElapsedEventArgs aArgs)
        {
            if (_vibratorSpeeds.SequenceEqual(NullSpeed))
            {
                _stopUpdateCommandSource.Cancel();
                _updateValueTimer.Enabled = false;
            }

            // We'll have to use an internal token here since this is timer triggered.
            if (await Interface.WriteValueAsync(ButtplugConsts.DefaultMsgId,
                (uint)MysteryVibeBluetoothInfo.Chrs.MotorControl,
                _vibratorSpeeds, false, _stopUpdateCommandSource.Token).ConfigureAwait(false) is Error)
            {
                BpLogger.Error($"Cannot send update to {Name}, device may stop moving.");
                _updateValueTimer.Enabled = false;
            }
        }

        private async Task<ButtplugMessage> HandleStopDeviceCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            BpLogger.Debug($"Stopping Device {Name}");
            return await HandleSingleMotorVibrateCmd(new SingleMotorVibrateCmd(aMsg.DeviceIndex, 0, aMsg.Id), aToken).ConfigureAwait(false);
        }

        private async Task<ButtplugMessage> HandleSingleMotorVibrateCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckMessageHandler<SingleMotorVibrateCmd>(aMsg);

            return await HandleVibrateCmd(
                VibrateCmd.Create(cmdMsg.DeviceIndex, cmdMsg.Id, cmdMsg.Speed, 6), aToken).ConfigureAwait(false);
        }

        private Task<ButtplugMessage> HandleVibrateCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckGenericMessageHandler<VibrateCmd>(aMsg, 6);

            var newVibratorSpeeds = (byte[])_vibratorSpeeds.Clone();

            foreach (var v in cmdMsg.Speeds)
            {
                newVibratorSpeeds[v.Index] = (byte)(v.Speed * MaxSpeed);
            }

            if (newVibratorSpeeds.SequenceEqual(_vibratorSpeeds))
            {
                return Task.FromResult(new Ok(aMsg.Id) as ButtplugMessage);
            }

            _vibratorSpeeds = newVibratorSpeeds;

            if (!_updateValueTimer.Enabled)
            {
                // Make a new stop token
                _stopUpdateCommandSource = new CancellationTokenSource();

                // Run the update handler once to start the command
                MysteryVibeUpdateHandler(null, null);

                // Start the timer to it will keep updating
                _updateValueTimer.Enabled = true;
            }

            return Task.FromResult(new Ok(aMsg.Id) as ButtplugMessage);
        }
    }
}