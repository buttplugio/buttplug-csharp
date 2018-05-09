using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;

namespace Buttplug.Server.Bluetooth.Devices
{
    internal class LovenseRev1BluetoothInfo : IBluetoothDeviceInfo
    {
        public enum Chrs : uint
        {
            Tx = 0,
            Rx,
        }

        public Guid[] Services { get; } = { new Guid("0000fff0-0000-1000-8000-00805f9b34fb") };

        public string[] Names { get; } =
        {
            // Nora
            "LVS-A011", "LVS-C011",

            // Max
            "LVS-B011",

            // Ambi
            "LVS-L009",
        };

        public Guid[] Characteristics { get; } =
        {
            // tx characteristic
            new Guid("0000fff2-0000-1000-8000-00805f9b34fb"),

            // rx characteristic
            new Guid("0000fff1-0000-1000-8000-00805f9b34fb"),
        };

        public IButtplugDevice CreateDevice(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
        {
            return new Lovense(aLogManager, aInterface, this);
        }
    }

    internal class LovenseRev2BluetoothInfo : IBluetoothDeviceInfo
    {
        public enum Chrs : uint
        {
            Tx = 0,
            Rx,
        }

        public Guid[] Services { get; } = { new Guid("6e400001-b5a3-f393-e0a9-e50e24dcca9e") };

        public string[] Names { get; } =
        {
            // Lush
            "LVS-S001",

            // Hush
            "LVS-Z001",

            // Hush
            "LVS_Z001",
        };

        public Guid[] Characteristics { get; } =
        {
            // tx characteristic
            new Guid("6e400002-b5a3-f393-e0a9-e50e24dcca9e"),

            // rx characteristic
            new Guid("6e400003-b5a3-f393-e0a9-e50e24dcca9e"),
        };

        public IButtplugDevice CreateDevice(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
        {
            return new Lovense(aLogManager, aInterface, this);
        }
    }

    internal class LovenseRev3BluetoothInfo : IBluetoothDeviceInfo
    {
        public enum Chrs : uint
        {
            Tx = 0,
            Rx,
        }

        public Guid[] Services { get; } = { new Guid("50300001-0024-4bd4-bbd5-a6920e4c5653") };

        public string[] Names { get; } =
        {
            // Edge
            "LVS-P36",
        };

        public Guid[] Characteristics { get; } =
        {
            // tx characteristic
            new Guid("50300002-0024-4bd4-bbd5-a6920e4c5653"),

            // rx characteristic
            new Guid("50300003-0024-4bd4-bbd5-a6920e4c5653"),
        };

        public IButtplugDevice CreateDevice(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
        {
            return new Lovense(aLogManager, aInterface, this);
        }
    }

    internal class LovenseRev4BluetoothInfo : IBluetoothDeviceInfo
    {
        public enum Chrs : uint
        {
            Tx = 0,
            Rx,
        }

        public Guid[] Services { get; } = { new Guid("57300001-0023-4bd4-bbd5-a6920e4c5653") };

        public string[] Names { get; } =
        {
            // Domi
            "LVS-Domi37",

            // Domi
            "LVS-Domi38",

            // Domi
            "LVS-Domi39",

            // Domi
            "LVS-Domi40",

            // Domi
            "LVS-Domi41",
        };

        public Guid[] Characteristics { get; } =
        {
            // tx characteristic
            new Guid("57300002-0023-4bd4-bbd5-a6920e4c5653"),

            // rx characteristic
            new Guid("57300003-0023-4bd4-bbd5-a6920e4c5653"),
        };

        public IButtplugDevice CreateDevice(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
        {
            return new Lovense(aLogManager, aInterface, this);
        }
    }

    internal class LovenseRev5BluetoothInfo : IBluetoothDeviceInfo
    {
        public enum Chrs : uint
        {
            Tx = 0,
            Rx,
        }

        public Guid[] Services { get; } = { new Guid("5a300001-0024-4bd4-bbd5-a6920e4c5653") };

        public string[] Names { get; } =
        {
            // Hush. Again.
            "LVS-Z36",
        };

        public Guid[] Characteristics { get; } =
        {
            // tx characteristic
            new Guid("5a300002-0024-4bd4-bbd5-a6920e4c5653"),

            // rx characteristic
            new Guid("5a300003-0024-4bd4-bbd5-a6920e4c5653"),
        };

        public IButtplugDevice CreateDevice(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
        {
            return new Lovense(aLogManager, aInterface, this);
        }
    }

    internal class LovenseRev6BluetoothInfo : IBluetoothDeviceInfo
    {
        public enum Chrs : uint
        {
            Tx = 0,
            Rx,
        }

        public Guid[] Services { get; } = { new Guid("50300001-0023-4bd4-bbd5-a6920e4c5653") };

        public string[] Names { get; } =
        {
            "LVS-Edge37",
            "LVS-Edge38",
        };

        public Guid[] Characteristics { get; } =
        {
            // tx characteristic
            new Guid("50300002-0023-4bd4-bbd5-a6920e4c5653"),

            // rx characteristic
            new Guid("50300003-0023-4bd4-bbd5-a6920e4c5653"),
        };

        public IButtplugDevice CreateDevice(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
        {
            return new Lovense(aLogManager, aInterface, this);
        }
    }

    internal class LovenseRev7BluetoothInfo : IBluetoothDeviceInfo
    {
        public enum Chrs : uint
        {
            Tx = 0,
            Rx,
        }

        public Guid[] Services { get; } = { new Guid("53300001-0023-4bd4-bbd5-a6920e4c5653") };

        public string[] Names { get; } =
        {
            // Lush. Again.
            "LVS-S35",
            "LVS-Lush41",
        };

        public Guid[] Characteristics { get; } =
        {
            // tx characteristic
            new Guid("53300002-0023-4bd4-bbd5-a6920e4c5653"),

            // rx characteristic
            new Guid("53300003-0023-4bd4-bbd5-a6920e4c5653"),
        };

        public IButtplugDevice CreateDevice(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
        {
            return new Lovense(aLogManager, aInterface, this);
        }
    }

    internal class LovenseRev8BluetoothInfo : IBluetoothDeviceInfo
    {
        public enum Chrs : uint
        {
            Tx = 0,
            Rx,
        }

        public Guid[] Services { get; } = { new Guid("5a300001-0023-4bd4-bbd5-a6920e4c5653") };

        public string[] Names { get; } =
        {
            // Hush. Again.
            "LVS-Hush41",
        };

        public Guid[] Characteristics { get; } =
        {
            // tx characteristic
            new Guid("5a300002-0023-4bd4-bbd5-a6920e4c5653"),

            // rx characteristic
            new Guid("5a300003-0023-4bd4-bbd5-a6920e4c5653"),
        };

        public IButtplugDevice CreateDevice(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
        {
            return new Lovense(aLogManager, aInterface, this);
        }
    }

    internal class Lovense : ButtplugBluetoothDevice
    {
        private static readonly Dictionary<string, string> FriendlyNames = new Dictionary<string, string>
        {
            { "LVS-A011", "Nora" },
            { "LVS-C011", "Nora" },
            { "LVS-B011", "Max" },
            { "LVS-L009", "Ambi" },
            { "LVS-S001", "Lush" },
            { "LVS-S35", "Lush" },
            { "LVS-Lush41", "Lush" },
            { "LVS-Z36", "Hush" },
            { "LVS-Hush41", "Hush" },
            { "LVS-Z001", "Hush" },
            { "LVS_Z001", "Hush" },
            { "LVS-Domi37", "Domi" },
            { "LVS-Domi38", "Domi" },
            { "LVS-Domi39", "Domi" },
            { "LVS-Domi40", "Domi" },
            { "LVS-Domi41", "Domi" },
            { "LVS-P36", "Edge" },
            { "LVS-Edge37", "Edge" },
            { "LVS-Edge38", "Edge" },
        };

        private readonly uint _vibratorCount = 1;
        private readonly double[] _vibratorSpeeds = { 0, 0 };
        private bool _clockwise = true;
        private double _rotateSpeed;

        public Lovense(IButtplugLogManager aLogManager,
                       IBluetoothDeviceInterface aInterface,
                       IBluetoothDeviceInfo aInfo)
            : base(aLogManager,
                   $"Lovense Device ({FriendlyNames[aInterface.Name]})",
                   aInterface,
                   aInfo)
        {
            if (FriendlyNames[aInterface.Name] == "Edge")
            {
                _vibratorCount++;
            }

            MsgFuncs.Add(typeof(SingleMotorVibrateCmd), new ButtplugDeviceWrapper(HandleSingleMotorVibrateCmd));
            MsgFuncs.Add(typeof(VibrateCmd), new ButtplugDeviceWrapper(HandleVibrateCmd, new MessageAttributes() { FeatureCount = _vibratorCount }));
            MsgFuncs.Add(typeof(StopDeviceCmd), new ButtplugDeviceWrapper(HandleStopDeviceCmd));

            if (FriendlyNames[aInterface.Name] == "Nora")
            {
                MsgFuncs.Add(typeof(RotateCmd), new ButtplugDeviceWrapper(HandleRotateCmd, new MessageAttributes() { FeatureCount = 1 }));
            }
        }

        private async Task<ButtplugMessage> HandleStopDeviceCmd(ButtplugDeviceMessage aMsg)
        {
            BpLogger.Debug("Stopping Device " + Name);

            if (FriendlyNames[Interface.Name] == "Nora")
            {
                await HandleRotateCmd(new RotateCmd(aMsg.DeviceIndex,
                    new List<RotateCmd.RotateSubcommand> { new RotateCmd.RotateSubcommand(0, 0, _clockwise) },
                    aMsg.Id));
            }

            return await HandleSingleMotorVibrateCmd(new SingleMotorVibrateCmd(aMsg.DeviceIndex, 0, aMsg.Id));
        }

        private async Task<ButtplugMessage> HandleSingleMotorVibrateCmd(ButtplugDeviceMessage aMsg)
        {
            if (!(aMsg is SingleMotorVibrateCmd cmdMsg))
            {
                return BpLogger.LogErrorMsg(aMsg.Id, Error.ErrorClass.ERROR_DEVICE, "Wrong Handler");
            }

            var speeds = new List<VibrateCmd.VibrateSubcommand>();
            for (uint i = 0; i < _vibratorCount; i++)
            {
                speeds.Add(new VibrateCmd.VibrateSubcommand(i, cmdMsg.Speed));
            }

            return await HandleVibrateCmd(new VibrateCmd(cmdMsg.DeviceIndex, speeds, cmdMsg.Id));
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
                    Info.Characteristics[(uint)LovenseRev1BluetoothInfo.Chrs.Tx],
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
                   Info.Characteristics[(uint)LovenseRev1BluetoothInfo.Chrs.Tx],
                   Encoding.ASCII.GetBytes($"RotateChange;"));
            }

            if (!speedChange)
            {
                return new Ok(cmdMsg.Id);
            }

            return await Interface.WriteValue(aMsg.Id,
                Info.Characteristics[(uint)LovenseRev1BluetoothInfo.Chrs.Tx],
                Encoding.ASCII.GetBytes($"Rotate:{(int)(_rotateSpeed * 20)};"));
        }
    }
}
