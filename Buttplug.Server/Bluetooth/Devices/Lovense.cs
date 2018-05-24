using Buttplug.Core;
using Buttplug.Core.Messages;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

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

        private uint _vibratorCount = 1;
        private readonly double[] _vibratorSpeeds = { 0, 0 };
        private bool _clockwise = true;
        private double _rotateSpeed;
        private LovenseDeviceType _deviceType = LovenseDeviceType.Unknown;

        public Lovense(IButtplugLogManager aLogManager,
                       IBluetoothDeviceInterface aInterface,
                       IBluetoothDeviceInfo aInfo)
            : base(aLogManager,
                   "Lovense Unknown Device",
                   aInterface,
                   aInfo)
        {
            MsgFuncs.Add(typeof(SingleMotorVibrateCmd), new ButtplugDeviceWrapper(HandleSingleMotorVibrateCmd));
            MsgFuncs.Add(typeof(VibrateCmd), new ButtplugDeviceWrapper(HandleVibrateCmd, new MessageAttributes() { FeatureCount = _vibratorCount }));
            MsgFuncs.Add(typeof(StopDeviceCmd), new ButtplugDeviceWrapper(HandleStopDeviceCmd));
        }

        public override async Task<ButtplugMessage> Initialize()
        {
            BpLogger.Trace($"Initializing {Name}");

            // Subscribing to read updates
            await Interface.SubscribeToUpdates();

            // Retreiving device type info for identification.
            var writeMsg = await Interface.WriteValue(ButtplugConsts.SystemMsgId, Encoding.ASCII.GetBytes($"DeviceType;"), true);
            if (writeMsg is Error)
            {
                BpLogger.Error($"Error requesting device info from Lovense {Name}");
                return writeMsg;
            }

            var (msg, result) = await Interface.ReadValue(ButtplugConsts.SystemMsgId);
            if (msg is Ok)
            {
                // Expected Format X:YY:ZZZZZZZZZZZZ X is device type leter YY is firmware version Z
                // is bluetooth address
                var deviceInfoString = Encoding.ASCII.GetString(result);
                var deviceInfo = deviceInfoString.Split(':');

                // If we don't get back the amount of tokens we expect, identify as unknown, log, bail.
                if (deviceInfo.Length != 3 || deviceInfo[0].Length != 1)
                {
                    return BpLogger.LogErrorMsg(ButtplugConsts.SystemMsgId, Error.ErrorClass.ERROR_DEVICE,
                        $"Unknown Lovense DeviceType of {deviceInfoString} found. Please report to Buttplug Developers by filing an issue at https://github.com/metafetish/buttplug/");
                }

                var deviceTypeLetter = deviceInfo[0][0];
                int.TryParse(deviceInfo[1], out var deviceVersion);
                BpLogger.Trace($"Lovense DeviceType Return: {deviceInfo}");
                if (!Enum.IsDefined(typeof(LovenseDeviceType), (uint)deviceTypeLetter))
                {
                    // If we don't know what device this is, just assume it has a single vibrator,
                    // call it unknown, log something.
                    return BpLogger.LogErrorMsg(ButtplugConsts.SystemMsgId, Error.ErrorClass.ERROR_DEVICE,
                        $"Unknown Lovense Device of Type {deviceTypeLetter} found. Please report to Buttplug Developers by filing an issue at https://github.com/metafetish/buttplug/");
                }

                Name = $"Lovense {Enum.GetName(typeof(LovenseDeviceType), (uint)deviceTypeLetter)} v{deviceVersion}";

                _deviceType = (LovenseDeviceType)deviceTypeLetter;
            }
            else
            {
                // TODO Bug #417 - Older lovense devices don't respond to DeviceType; query
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

                    default:
                        _deviceType = LovenseDeviceType.Unknown;
                        break;
                }

                Name = $"Lovense {Enum.GetName(typeof(LovenseDeviceType), (uint)_deviceType)}";
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
                    MsgFuncs.Remove(typeof(VibrateCmd));
                    MsgFuncs.Add(typeof(VibrateCmd), new ButtplugDeviceWrapper(HandleVibrateCmd, new MessageAttributes() { FeatureCount = _vibratorCount }));
                    break;

                case LovenseDeviceType.Nora:

                    // Nora has a rotator
                    MsgFuncs.Add(typeof(RotateCmd), new ButtplugDeviceWrapper(HandleRotateCmd, new MessageAttributes() { FeatureCount = 1 }));
                    break;
            }

            return new Ok(ButtplugConsts.SystemMsgId);
        }

        private async Task<ButtplugMessage> HandleStopDeviceCmd(ButtplugDeviceMessage aMsg)
        {
            BpLogger.Debug("Stopping Device " + Name);

            if (_deviceType == LovenseDeviceType.Nora)
            {
                await HandleRotateCmd(RotateCmd.Create(aMsg.DeviceIndex, aMsg.Id, 0, _clockwise, 1));
            }

            return await HandleSingleMotorVibrateCmd(new SingleMotorVibrateCmd(aMsg.DeviceIndex, 0, aMsg.Id));
        }

        private async Task<ButtplugMessage> HandleSingleMotorVibrateCmd(ButtplugDeviceMessage aMsg)
        {
            if (!(aMsg is SingleMotorVibrateCmd cmdMsg))
            {
                return BpLogger.LogErrorMsg(aMsg.Id, Error.ErrorClass.ERROR_DEVICE, "Wrong Handler");
            }

            return await HandleVibrateCmd(VibrateCmd.Create(cmdMsg.DeviceIndex, cmdMsg.Id, cmdMsg.Speed, _vibratorCount));
        }

        private async Task<ButtplugMessage> HandleVibrateCmd(ButtplugDeviceMessage aMsg)
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
                    Encoding.ASCII.GetBytes($"Vibrate{vId}:{(int)(_vibratorSpeeds[v.Index] * 20)};"));

                if (!(res is Ok))
                {
                    return res;
                }
            }

            return new Ok(cmdMsg.Id);
        }

        private async Task<ButtplugMessage> HandleRotateCmd(ButtplugDeviceMessage aMsg)
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
                   Encoding.ASCII.GetBytes($"RotateChange;"));
            }

            if (!speedChange)
            {
                return new Ok(cmdMsg.Id);
            }

            return await Interface.WriteValue(aMsg.Id,
                Encoding.ASCII.GetBytes($"Rotate:{(int)(_rotateSpeed * 20)};"));
        }
    }
}
