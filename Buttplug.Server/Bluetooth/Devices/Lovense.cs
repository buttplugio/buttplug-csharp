// <copyright file="Lovense.cs" company="Nonpolynomial Labs LLC">
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

namespace Buttplug.Server.Bluetooth.Devices
{
    internal class LovenseBluetoothInfo : IBluetoothDeviceInfo
    {
        public enum Chrs : uint
        {
            Tx = 0,
            Rx,
        }

        // Use NamePrefix instead
        public static string[] NamesInfo { get; } = { };

        // Use autocreated TX/RX characteristics
        public static Dictionary<uint, Guid> CharacteristicsInfo { get; } = new Dictionary<uint, Guid>();

        public static Guid[] ServicesInfo { get; } =
        {
            new Guid("0000fff0-0000-1000-8000-00805f9b34fb"),
            new Guid("6e400001-b5a3-f393-e0a9-e50e24dcca9e"),
            new Guid("50300001-0024-4bd4-bbd5-a6920e4c5653"),
            new Guid("57300001-0023-4bd4-bbd5-a6920e4c5653"),
            new Guid("5a300001-0024-4bd4-bbd5-a6920e4c5653"),
            new Guid("50300001-0023-4bd4-bbd5-a6920e4c5653"),
            new Guid("53300001-0023-4bd4-bbd5-a6920e4c5653"),
            new Guid("5a300001-0023-4bd4-bbd5-a6920e4c5653"),
        };

        public static string[] NamePrefixesInfo { get; } =
        {
            "LVS",
        };

        public Dictionary<uint, Guid> Characteristics { get; } = CharacteristicsInfo;

        public Guid[] Services { get; } = ServicesInfo;

        public string[] Names { get; } = NamesInfo;

        public string[] NamePrefixes { get; } = NamePrefixesInfo;

        public IButtplugDevice CreateDevice(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
        {
            return new Lovense(aLogManager, aInterface, this);
        }
    }

    internal class Lovense : ButtplugBluetoothDevice
    {
        // Identify Lovense devices against the character we expect to get back from the DeviceType
        // read. See https://docs.buttplug.io/stpihkal for protocol info.
        public enum LovenseDeviceType : uint
        {
            Max = 'B',

            // Nora is A or C. Set to A here, then on type check, convert C to A.
            Nora = 'A',

            Ambi = 'L',
            Lush = 'S',
            Hush = 'Z',
            Domi = 'W',
            Edge = 'P',
            Osci = 'O',
            Unknown = 0,
        }

        private readonly double[] _vibratorSpeeds = { 0, 0 };
        private uint _vibratorCount = 1;
        private bool _clockwise = true;
        private double _rotateSpeed;
        private LovenseDeviceType _deviceType = LovenseDeviceType.Unknown;
        private string _lastNotifyReceived = string.Empty;

        public Lovense(IButtplugLogManager aLogManager,
                       IBluetoothDeviceInterface aInterface,
                       IBluetoothDeviceInfo aInfo)
            : base(aLogManager,
                   "Lovense Unknown Device",
                   aInterface,
                   aInfo)
        {
        }

        public override async Task<ButtplugMessage> Initialize(CancellationToken aToken)
        {
            BpLogger.Trace($"Initializing {Name}");

            // Subscribing to read updates
            await Interface.SubscribeToUpdates();
            Interface.BluetoothNotifyReceived += NotifyReceived;

            // Retreiving device type info for identification.
            var writeMsg = await Interface.WriteValue(ButtplugConsts.SystemMsgId, Encoding.ASCII.GetBytes($"DeviceType;"), true, aToken);
            if (writeMsg is Error)
            {
                BpLogger.Error($"Error requesting device info from Lovense {Name}");
                return writeMsg;
            }

            var (msg, result) = await Interface.ReadValue(ButtplugConsts.SystemMsgId, aToken);
            var deviceInfoString = string.Empty;

            if (msg is Ok)
            {
                deviceInfoString = Encoding.ASCII.GetString(result);
            }
            else
            {
                // The device info notification isn't available immediately.
                // TODO Turn this into a task semaphore with cancellation/timeout, let system handle check timing.
                int timeout = 500;
                while (timeout > 0)
                {
                    if (_lastNotifyReceived != string.Empty)
                    {
                        deviceInfoString = _lastNotifyReceived;
                        break;
                    }

                    timeout -= 5;
                    await Task.Delay(5);
                }
            }

            if (deviceInfoString != string.Empty)
            {
                BpLogger.Debug($"Received device query return for {Interface.Name}");

                // Expected Format X:YY:ZZZZZZZZZZZZ X is device type leter YY is firmware version Z
                // is bluetooth address
                var deviceInfo = deviceInfoString.Split(':');

                // If we don't get back the amount of tokens we expect, identify as unknown, log, bail.
                if (deviceInfo.Length != 3 || deviceInfo[0].Length != 1)
                {
                    return BpLogger.LogErrorMsg(ButtplugConsts.SystemMsgId, Error.ErrorClass.ERROR_DEVICE,
                        $"Unknown Lovense DeviceType of {deviceInfoString} found. Please report to Buttplug Developers by filing an issue at https://github.com/buttplugio/buttplug/");
                }

                var deviceTypeLetter = deviceInfo[0][0];
                if (deviceTypeLetter == 'C')
                {
                    deviceTypeLetter = 'A';
                }

                int.TryParse(deviceInfo[1], out var deviceVersion);
                BpLogger.Trace($"Lovense DeviceType Return: {deviceTypeLetter}");
                if (!Enum.IsDefined(typeof(LovenseDeviceType), (uint)deviceTypeLetter))
                {
                    // If we don't know what device this is, just assume it has a single vibrator,
                    // call it unknown, log something.
                    AddCommonMessages();
                    BpLogger.LogErrorMsg(new Error($"Unknown Lovense Device of Type {deviceTypeLetter} found. Please report to Buttplug Developers by filing an issue at https://github.com/buttplugio/buttplug/",
                        Error.ErrorClass.ERROR_DEVICE, ButtplugConsts.SystemMsgId));
                    return new Ok(ButtplugConsts.SystemMsgId);
                }

                Name = $"Lovense {Enum.GetName(typeof(LovenseDeviceType), (uint)deviceTypeLetter)} v{deviceVersion}";

                _deviceType = (LovenseDeviceType)deviceTypeLetter;
            }
            else
            {
                // If we for some reason don't get a device info query back, use fallback method.
                //
                // TODO Remove this branch at some point? Not sure we'll need it now since device queries seem stable.
                BpLogger.Warn($"Error retreiving device info from Lovense {Name}, using fallback method");

                // Some of the older devices seem to have issues with info lookups? Not sure why, so
                // for now use fallback method.
                switch (Interface.Name.Substring(0, 6))
                {
                    case "LVS-B0":
                        _deviceType = LovenseDeviceType.Max;
                        break;

                    case "LVS-A0":
                    case "LVS-C0":
                        _deviceType = LovenseDeviceType.Nora;
                        break;
                    case "LVS-L0":
                        _deviceType = LovenseDeviceType.Ambi;
                        break;
                    default:
                        _deviceType = LovenseDeviceType.Unknown;
                        break;
                }

                Name = $"Lovense {Enum.GetName(typeof(LovenseDeviceType), (uint)_deviceType)} v{Interface.Name.Substring(Interface.Name.Length - 2)}";
            }

            if (_deviceType == LovenseDeviceType.Unknown)
            {
                BpLogger.Error("Lovense device type unknown, treating as single vibrator device. Please contact developers for more info.");
            }

            switch (_deviceType)
            {
                case LovenseDeviceType.Edge:

                    // Edge has 2 vibrators
                    _vibratorCount++;
                    break;

                case LovenseDeviceType.Nora:

                    // Nora has a rotator
                    MsgFuncs.Add(typeof(RotateCmd), new ButtplugDeviceMessageHandler(HandleRotateCmd, new MessageAttributes() { FeatureCount = 1 }));
                    break;
            }

            // Common messages.
            AddCommonMessages();

            return new Ok(ButtplugConsts.SystemMsgId);
        }

        private void AddCommonMessages()
        {
            MsgFuncs.Add(typeof(LovenseCmd), new ButtplugDeviceMessageHandler(HandleLovenseCmd));
            MsgFuncs.Add(typeof(StopDeviceCmd), new ButtplugDeviceMessageHandler(HandleStopDeviceCmd));

            // At present there are no Lovense devices that do not have at least one vibrator.
            MsgFuncs.Add(typeof(VibrateCmd), new ButtplugDeviceMessageHandler(HandleVibrateCmd, new MessageAttributes() { FeatureCount = _vibratorCount }));
            MsgFuncs.Add(typeof(SingleMotorVibrateCmd), new ButtplugDeviceMessageHandler(HandleSingleMotorVibrateCmd));
        }

        private void NotifyReceived(object sender, BluetoothNotifyEventArgs args)
        {
            var data = Encoding.ASCII.GetString(args.bytes);

            //BpLogger.Trace(data);
            _lastNotifyReceived = data;
        }

        private async Task<ButtplugMessage> HandleStopDeviceCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            BpLogger.Debug("Stopping Device " + Name);

            if (_deviceType == LovenseDeviceType.Nora)
            {
                await HandleRotateCmd(RotateCmd.Create(aMsg.DeviceIndex, aMsg.Id, 0, _clockwise, 1), aToken);
            }

            return await HandleSingleMotorVibrateCmd(new SingleMotorVibrateCmd(aMsg.DeviceIndex, 0, aMsg.Id), aToken);
        }

        private async Task<ButtplugMessage> HandleSingleMotorVibrateCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            if (!(aMsg is SingleMotorVibrateCmd cmdMsg))
            {
                return BpLogger.LogErrorMsg(aMsg.Id, Error.ErrorClass.ERROR_DEVICE, "Wrong Handler");
            }

            return await HandleVibrateCmd(VibrateCmd.Create(cmdMsg.DeviceIndex, cmdMsg.Id, cmdMsg.Speed, _vibratorCount), aToken);
        }

        private async Task<ButtplugMessage> HandleVibrateCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            if (!(aMsg is VibrateCmd cmdMsg))
            {
                return BpLogger.LogErrorMsg(aMsg.Id, Error.ErrorClass.ERROR_DEVICE, "Wrong Handler");
            }

            if (cmdMsg.Speeds.Count == 0 || cmdMsg.Speeds.Count > _vibratorCount)
            {
                return new Error(
                    _vibratorCount == 1 ? "VibrateCmd requires 1 vector for this device." :
                                         $"VibrateCmd requires between 1 and {_vibratorCount} vectors for this device.",
                    Error.ErrorClass.ERROR_DEVICE,
                    cmdMsg.Id);
            }

            foreach (var v in cmdMsg.Speeds)
            {
                if (v.Index >= _vibratorCount)
                {
                    return new Error(
                        $"Index {v.Index} is out of bounds for VibrateCmd for this device.",
                        Error.ErrorClass.ERROR_DEVICE,
                        cmdMsg.Id);
                }

                if (Math.Abs(v.Speed - _vibratorSpeeds[v.Index]) < 0.0001)
                {
                    continue;
                }

                _vibratorSpeeds[v.Index] = v.Speed;
                var vId = _vibratorCount == 1 ? string.Empty : string.Empty + (v.Index + 1);
                var res = await Interface.WriteValue(aMsg.Id,
                    Encoding.ASCII.GetBytes($"Vibrate{vId}:{(int)(_vibratorSpeeds[v.Index] * 20)};"), false, aToken);

                if (!(res is Ok))
                {
                    return res;
                }
            }

            return new Ok(cmdMsg.Id);
        }

        private async Task<ButtplugMessage> HandleLovenseCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            if (!(aMsg is LovenseCmd cmdMsg))
            {
                return BpLogger.LogErrorMsg(aMsg.Id, Error.ErrorClass.ERROR_DEVICE, "Wrong Handler");
            }

            return await Interface.WriteValue(aMsg.Id, Encoding.ASCII.GetBytes(cmdMsg.Command), false, aToken);
        }

        private async Task<ButtplugMessage> HandleRotateCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            if (!(aMsg is RotateCmd cmdMsg))
            {
                return BpLogger.LogErrorMsg(aMsg.Id, Error.ErrorClass.ERROR_DEVICE, "Wrong Handler");
            }

            var dirChange = false;
            var speedChange = false;

            if (cmdMsg.Rotations.Count != 1)
            {
                return new Error(
                    "RotateCmd requires 1 vector for this device.",
                    Error.ErrorClass.ERROR_DEVICE,
                    cmdMsg.Id);
            }

            foreach (var vi in cmdMsg.Rotations)
            {
                if (vi.Index != 0)
                {
                    return new Error(
                        $"Index {vi.Index} is out of bounds for RotateCmd for this device.",
                        Error.ErrorClass.ERROR_DEVICE,
                        cmdMsg.Id);
                }

                speedChange = Math.Abs(_rotateSpeed - vi.Speed) > 0.0001;
                _rotateSpeed = vi.Speed;
                dirChange = _clockwise != vi.Clockwise;
            }

            if (dirChange)
            {
                _clockwise = !_clockwise;
                await Interface.WriteValue(aMsg.Id,
                   Encoding.ASCII.GetBytes($"RotateChange;"), false, aToken);
            }

            if (!speedChange)
            {
                return new Ok(cmdMsg.Id);
            }

            return await Interface.WriteValue(aMsg.Id,
                Encoding.ASCII.GetBytes($"Rotate:{(int)(_rotateSpeed * 20)};"), false, aToken);
        }
    }
}
