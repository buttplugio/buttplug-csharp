// <copyright file="Cueme.cs" company="Nonpolynomial Labs LLC">
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
    internal class CuemeBluetoothInfo : IBluetoothDeviceInfo
    {
        public enum Chrs : uint
        {
            Tx = 0,
        }

        public Guid[] Services { get; } = { new Guid("0000fff0-0000-1000-8000-00805f9b34fb") };

        public string[] NamePrefixes { get; } = { "FUNCODE_" };

        public string[] Names { get; } = { };

        public Dictionary<uint, Guid> Characteristics { get; } = new Dictionary<uint, Guid>()
        {
            { (uint)Chrs.Tx, new Guid("0000fff1-0000-1000-8000-00805f9b34fb") },
        };

        public IButtplugDevice CreateDevice(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
        {
            return new Cueme(aLogManager, aInterface, this);
        }
    }

    internal class Cueme : ButtplugBluetoothDevice
    {
        internal struct CuemeType
        {
            public string Name;
            public uint VibeCount;
        }

        private static readonly Dictionary<uint, CuemeType> DevInfos = new Dictionary<uint, CuemeType>
        {
            { 1, new CuemeType() { Name = "Mens", VibeCount = 8 } },
            { 2, new CuemeType() { Name = "Bra", VibeCount = 8 } },
            { 3, new CuemeType() { Name = "Womens", VibeCount = 4 } },
        };

        private readonly CuemeType _devInfo;
        private double[] _vibratorSpeeds = { 0, 0, 0, 0, 0, 0, 0, 0 };
        internal static readonly double[] NullSpeed = { 0, 0, 0, 0, 0, 0, 0, 0 };
        private uint _vibeIndex;

        // When alternating vibes, switch twice a second
        internal static readonly uint DelayTimeMS = 500;

        private readonly System.Timers.Timer _updateValueTimer = new System.Timers.Timer();
        private CancellationTokenSource _stopUpdateCommandSource = new CancellationTokenSource();

        public Cueme(IButtplugLogManager aLogManager,
                       IBluetoothDeviceInterface aInterface,
                       IBluetoothDeviceInfo aInfo)
            : base(aLogManager,
                   "Cueme Unknown",
                   aInterface,
                   aInfo)
        {
            var bits = aInterface.Name.Split('_');
            if (bits.Length == 3 && uint.TryParse(bits[2], out var typeNum) && DevInfos.ContainsKey(typeNum))
            {
                _devInfo = DevInfos[typeNum];
            }
            else
            {
                BpLogger.Warn($"Cannot identify Cueme device {Name}, defaulting to Womens settings.");
                _devInfo = DevInfos[3];
            }

            Name = $"Cueme {_devInfo.Name}";

            // Create a new timer that wont fire any events just yet
            _updateValueTimer.Interval = DelayTimeMS;
            _updateValueTimer.Elapsed += CuemeUpdateHandler;
            _updateValueTimer.Enabled = false;
            aInterface.DeviceRemoved += OnDeviceRemoved;

            AddMessageHandler<StopDeviceCmd>(HandleStopDeviceCmd);
            AddMessageHandler<SingleMotorVibrateCmd>(HandleSingleMotorVibrateCmd);
            AddMessageHandler<VibrateCmd>(HandleVibrateCmd, new MessageAttributes { FeatureCount = _devInfo.VibeCount });
        }

        private void OnDeviceRemoved(object aEvent, EventArgs aArgs)
        {
            // Timer should be turned off on removal.
            _updateValueTimer.Enabled = false;

            // Clean up event handler for that magic day when devices manage to disconnect.
            Interface.DeviceRemoved -= OnDeviceRemoved;
        }

        private async void CuemeUpdateHandler(object aEvent, ElapsedEventArgs aArgs)
        {
            if (aArgs == null && !_updateValueTimer.Enabled)
            {
                _updateValueTimer.Enabled = true;
            }

            if (_vibratorSpeeds.SequenceEqual(NullSpeed))
            {
                _updateValueTimer.Enabled = false;
                _vibeIndex = 0;
            }

            var data = Convert.ToByte((int)(_vibratorSpeeds[_vibeIndex] * 15));
            if (data != 0x00)
            {
                data |= (byte)((byte)(_vibeIndex + 1) << 4);
            }

            // We'll have to use an internal token here since this is timer triggered.
            if (await Interface.WriteValueAsync(ButtplugConsts.DefaultMsgId,
                (uint)CuemeBluetoothInfo.Chrs.Tx, new[] { data },
                false, _stopUpdateCommandSource.Token).ConfigureAwait(false) is Error)
            {
                BpLogger.Error($"Cannot send update to Cueme {_devInfo.Name}, device may stick on a single vibrator.");
                _updateValueTimer.Enabled = false;
            }

            if (!_updateValueTimer.Enabled || aArgs == null)
            {
                return;
            }

            // Move to the next active vibe
            var nextVibe = _vibeIndex;
            while (true)
            {
                nextVibe++;

                // Wrap back to 0
                if (nextVibe == _devInfo.VibeCount)
                {
                    nextVibe = 0;
                }

                if (Math.Abs(_vibratorSpeeds[nextVibe]) > 0.0)
                {
                    _vibeIndex = nextVibe;
                    break;
                }

                if (nextVibe == _vibeIndex)
                {
                    // Stop the timer: there's only one vibe running
                    _updateValueTimer.Enabled = false;
                    break;
                }
            }
        }

        private async Task<ButtplugMessage> HandleStopDeviceCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            BpLogger.Debug("Stopping Device " + Name);
            return await HandleSingleMotorVibrateCmd(new SingleMotorVibrateCmd(aMsg.DeviceIndex, 0, aMsg.Id), aToken).ConfigureAwait(false);
        }

        private async Task<ButtplugMessage> HandleSingleMotorVibrateCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckMessageHandler<SingleMotorVibrateCmd>(aMsg);

            return await HandleVibrateCmd(VibrateCmd.Create(cmdMsg.DeviceIndex, cmdMsg.Id, cmdMsg.Speed, _devInfo.VibeCount), aToken).ConfigureAwait(false);
        }

        private Task<ButtplugMessage> HandleVibrateCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckGenericMessageHandler<VibrateCmd>(aMsg, _devInfo.VibeCount);

            var newVibratorSpeeds = (double[])_vibratorSpeeds.Clone();

            foreach (var v in cmdMsg.Speeds)
            {
                if (Math.Abs(v.Speed - newVibratorSpeeds[v.Index]) < 0.001)
                {
                    continue;
                }

                newVibratorSpeeds[v.Index] = v.Speed;
            }

            if (newVibratorSpeeds.SequenceEqual(_vibratorSpeeds))
            {
                return Task.FromResult(new Ok(aMsg.Id) as ButtplugMessage);
            }

            _vibratorSpeeds = newVibratorSpeeds;

            CuemeUpdateHandler(null, null);

            return Task.FromResult(new Ok(aMsg.Id) as ButtplugMessage);
        }
    }
}