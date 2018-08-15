﻿// <copyright file="Kiiroo.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;
using Buttplug.Server.Util;
using JetBrains.Annotations;

namespace Buttplug.Server.Bluetooth.Devices
{
    internal class KiirooBluetoothInfo : IBluetoothDeviceInfo
    {
        public enum Chrs : uint
        {
            Rx = 0,
            Tx,
            Cmd,
            Cmd2,
        }

        public string[] Names { get; } = { "ONYX", "PEARL" };

        public string[] NamePrefixes { get; } = { };

        public Guid[] Services { get; } = { new Guid("49535343-fe7d-4ae5-8fa9-9fafd205e455") };

        public Dictionary<uint, Guid> Characteristics { get; } = new Dictionary<uint, Guid>()
        {
            // rx
            { (uint)Chrs.Rx, new Guid("49535343-1e4d-4bd9-ba61-23c647249616") },

            // tx
            { (uint)Chrs.Tx, new Guid("49535343-8841-43f4-a8d4-ecbe34729bb3") },

            // cmd
            { (uint)Chrs.Cmd, new Guid("49535343-aca3-481c-91ec-d85e28a60318") },

            // cmd2
            { (uint)Chrs.Cmd2, new Guid("49535343-6daa-4d02-abf6-19569aca69fe") },
        };

        public IButtplugDevice CreateDevice(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
        {
            return new Kiiroo(aLogManager, aInterface, this);
        }
    }

    internal class Kiiroo : ButtplugBluetoothDevice
    {
        private readonly object _onyxLock = new object();
        private double _deviceSpeed;
        private double _targetPosition;
        private double _currentPosition;
        private DateTime _targetTime = DateTime.Now;
        private DateTime _currentTime = DateTime.Now;
        private Timer _onyxTimer;

        public Kiiroo([NotNull] IButtplugLogManager aLogManager,
                      [NotNull] IBluetoothDeviceInterface aInterface,
                      [NotNull] IBluetoothDeviceInfo aInfo)
            : base(aLogManager,
                   $"Kiiroo {aInterface.Name}",
                   aInterface,
                   aInfo)
        {
            MsgFuncs.Add(typeof(KiirooCmd), new ButtplugDeviceMessageHandler(HandleKiirooRawCmd));
            MsgFuncs.Add(typeof(StopDeviceCmd), new ButtplugDeviceMessageHandler(HandleStopDeviceCmd));

            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (aInterface.Name == "PEARL")
            {
                MsgFuncs.Add(typeof(VibrateCmd), new ButtplugDeviceMessageHandler(HandleVibrateCmd,
                    new MessageAttributes() { FeatureCount = 1 }));
                MsgFuncs.Add(typeof(SingleMotorVibrateCmd), new ButtplugDeviceMessageHandler(HandleSingleMotorVibrateCmd));
            }
            else if (aInterface.Name == "ONYX")
            {
                MsgFuncs.Add(typeof(LinearCmd), new ButtplugDeviceMessageHandler(HandleLinearCmd,
                    new MessageAttributes() { FeatureCount = 1 }));
                MsgFuncs.Add(typeof(FleshlightLaunchFW12Cmd),
                    new ButtplugDeviceMessageHandler(HandleFleshlightLaunchFW12Cmd));
            }
        }

        private void OnBluetoothMessageReceived(object sender, BluetoothNotifyEventArgs aArgs)
        {
            // no-op, but required for the Onyx to work
        }

        public override async Task<ButtplugMessage> Initialize(CancellationToken aToken)
        {
            // Start listening for incoming
            Interface.BluetoothNotifyReceived += OnBluetoothMessageReceived;
            await Interface.SubscribeToUpdates((uint)KiirooBluetoothInfo.Chrs.Rx);

            // Mode select
            await Interface.WriteValue(ButtplugConsts.SystemMsgId,
                (uint)KiirooBluetoothInfo.Chrs.Cmd,
                new byte[] { 0x01, 0x00 }, true, aToken);

            // Set to start position
            await Interface.WriteValue(ButtplugConsts.SystemMsgId,
                (uint)KiirooBluetoothInfo.Chrs.Tx,
                new byte[] { 0x30, 0x2c }, true, aToken);

            if (Interface.Name != "ONYX")
            {
                return new Ok(ButtplugConsts.SystemMsgId);
            }

            // Onyx specific
            Interface.DeviceRemoved += (sender, args) =>
            {
                _onyxTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                _onyxTimer = null;
            };

            _currentTime = DateTime.Now;
            _onyxTimer = new Timer(OnOnyxTimer, null, 500, 500);

            return new Ok(ButtplugConsts.SystemMsgId);
        }

        public override void Disconnect()
        {
            if (Interface.Name == "ONYX")
            {
                _onyxTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                _onyxTimer = null;
            }

            Interface.Disconnect();
        }

        private async void OnOnyxTimer(object state)
        {
            lock (_onyxLock)
            {
                // Given the time since the last iteration and the difference in distance, work out have far we should move
                var distance = Math.Abs(_targetPosition - _currentPosition);
                var oldTime = new DateTime(_currentTime.Ticks);
                var nowTime = DateTime.Now;
                var delta = nowTime.Subtract(oldTime);

                if (delta.TotalMilliseconds < 50)
                {
                    // Skip. Don't flood BLE
                    return;
                }

                if (Convert.ToUInt32(distance * 4) == 0 && delta.TotalMilliseconds < 500)
                {
                    // Skip. We do want to occationally ping the Onyx, but only every half a second
                    return;
                }

                _currentTime = nowTime;

                if (_currentTime.CompareTo(_targetTime) >= 0 || Convert.ToUInt32(distance * 4) == 0)
                {
                    // We've overdue: jump to target
                    _currentPosition = _targetPosition;
                    _onyxTimer?.Change(500, 500);
                }
                else
                {
                    // The hard part: find the persentage time gone, then add that percentate of the movement delta
                    var delta2 = _targetTime.Subtract(oldTime);

                    var move = (Convert.ToDouble(delta.TotalMilliseconds) / (Convert.ToDouble(delta2.TotalMilliseconds) + 1)) * distance;
                    _currentPosition += move * (_targetPosition > _currentPosition ? 1 : -1);
                    _currentPosition = Math.Max(0, Math.Min(1, _currentPosition));
                }
            }

            var res = await HandleKiirooRawCmd(new KiirooCmd(0, Convert.ToUInt32(_currentPosition * 4), ButtplugConsts.SystemMsgId), default(CancellationToken));
            if (res is Error err)
            {
                BpLogger.Error(err.ErrorMessage);
            }
        }

        private async Task<ButtplugMessage> HandleStopDeviceCmd([NotNull] ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            // Right now, this is a nop. The Onyx doesn't have any sort of permanent movement state,
            // and its longest movement is like 150ms or so. The Pearl is supposed to vibrate but I've
            // never gotten that to work. So for now, we just return ok.
            BpLogger.Debug("Stopping Device " + Name);

            if (Interface.Name == "PEARL" && _deviceSpeed > 0)
            {
                return await HandleKiirooRawCmd(new KiirooCmd(aMsg.DeviceIndex, 0, aMsg.Id), aToken);
            }

            return new Ok(aMsg.Id);
        }

        private async Task<ButtplugMessage> HandleKiirooRawCmd([NotNull] ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            if (!(aMsg is KiirooCmd cmdMsg))
            {
                return BpLogger.LogErrorMsg(aMsg.Id, Error.ErrorClass.ERROR_DEVICE, "Wrong Handler");
            }

            return await Interface.WriteValue(cmdMsg.Id, (uint)KiirooBluetoothInfo.Chrs.Tx,
                Encoding.ASCII.GetBytes($"{cmdMsg.Position},\n"), false, aToken);
        }

        private async Task<ButtplugMessage> HandleSingleMotorVibrateCmd([NotNull] ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            if (!(aMsg is SingleMotorVibrateCmd cmdMsg) || Interface.Name != "PEARL")
            {
                return BpLogger.LogErrorMsg(aMsg.Id, Error.ErrorClass.ERROR_DEVICE, "Wrong Handler");
            }

            return await HandleVibrateCmd(VibrateCmd.Create(cmdMsg.DeviceIndex, cmdMsg.Id, cmdMsg.Speed, 1), aToken);
        }

        private async Task<ButtplugMessage> HandleVibrateCmd([NotNull] ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            if (!(aMsg is VibrateCmd cmdMsg) || Interface.Name != "PEARL")
            {
                return BpLogger.LogErrorMsg(aMsg.Id, Error.ErrorClass.ERROR_DEVICE, "Wrong Handler");
            }

            if (cmdMsg.Speeds.Count != 1)
            {
                return new Error(
                    "VibrateCmd requires 1 vector for this device.",
                    Error.ErrorClass.ERROR_DEVICE,
                    cmdMsg.Id);
            }

            foreach (var v in cmdMsg.Speeds)
            {
                if (v.Index != 0)
                {
                    return new Error(
                        $"Index {v.Index} is out of bounds for VibrateCmd for this device.",
                        Error.ErrorClass.ERROR_DEVICE,
                        cmdMsg.Id);
                }

                if (Math.Abs(_deviceSpeed - v.Speed) < 0.001)
                {
                    return new Ok(cmdMsg.Id);
                }

                _deviceSpeed = v.Speed;
            }

            return await HandleKiirooRawCmd(new KiirooCmd(aMsg.DeviceIndex, Convert.ToUInt16(_deviceSpeed * 4), aMsg.Id), aToken);
        }

        private Task<ButtplugMessage> HandleFleshlightLaunchFW12Cmd([NotNull] ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            if (!(aMsg is FleshlightLaunchFW12Cmd cmdMsg) || Interface.Name != "ONYX")
            {
                return Task.FromResult<ButtplugMessage>(BpLogger.LogErrorMsg(
                    aMsg.Id, Error.ErrorClass.ERROR_DEVICE, "Wrong Handler"));
            }

            var pos = Convert.ToDouble(cmdMsg.Position) / 99.0;
            var dur = Convert.ToUInt32(FleshlightHelper.GetDuration(Math.Abs(pos - _currentPosition), cmdMsg.Speed / 99));
            return HandleLinearCmd(LinearCmd.Create(cmdMsg.DeviceIndex, cmdMsg.Id, dur, pos, 1), aToken);
        }

        private Task<ButtplugMessage> HandleLinearCmd([NotNull] ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            if (!(aMsg is LinearCmd cmdMsg) || Interface.Name != "ONYX")
            {
                return Task.FromResult<ButtplugMessage>(BpLogger.LogErrorMsg(
                    aMsg.Id, Error.ErrorClass.ERROR_DEVICE, "Wrong Handler"));
            }

            if (cmdMsg.Vectors.Count != 1)
            {
                return Task.FromResult<ButtplugMessage>(new Error(
                    "LinearCmd requires 1 vector for this device.",
                    Error.ErrorClass.ERROR_DEVICE,
                    cmdMsg.Id));
            }

            foreach (var v in cmdMsg.Vectors)
            {
                if (v.Index != 0)
                {
                    return Task.FromResult<ButtplugMessage>(new Error(
                        $"Index {v.Index} is out of bounds for LinearCmd for this device.",
                        Error.ErrorClass.ERROR_DEVICE,
                        cmdMsg.Id));
                }

                // Invert the position
                lock (_onyxLock)
                {
                    _targetPosition = 1 - v.Position;
                    _currentTime = DateTime.Now;
                    _targetTime = DateTime.Now.AddMilliseconds(v.Duration);
                    _onyxTimer?.Change(0, 50);
                }
            }

            return Task.FromResult<ButtplugMessage>(new Ok(aMsg.Id));
        }
    }
}
