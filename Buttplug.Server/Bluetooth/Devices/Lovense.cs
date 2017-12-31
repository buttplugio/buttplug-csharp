﻿using System;
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

            // Hush Prototype
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
            // Edge
            "LVS-Domi37",
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
            // Hush. Again.
            "LVS-Edge37",
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

    internal class Lovense : ButtplugBluetoothDevice
    {
        private static Dictionary<string, string> friendlyNames = new Dictionary<string, string>()
        {
            { "LVS-A011", "Nora" },
            { "LVS-C011", "Nora" },
            { "LVS-B011", "Max" },
            { "LVS-L009", "Ambi" },
            { "LVS-S001", "Lush" },
            { "LVS-Z001", "Hush" },
            { "LVS_Z001", "Hush Prototype" },
            { "LVS-P36", "Edge" },
            { "LVS-Z36", "Hush" },
            { "LVS-Domi37", "Domi" },
            { "LVS-Edge37", "Edge" },
        };

        private bool _clockwise = true;
        private double _rotateSpeed = 0;
        private double _vibratorSpeed = 0;

        public Lovense(IButtplugLogManager aLogManager,
                       IBluetoothDeviceInterface aInterface,
                       IBluetoothDeviceInfo aInfo)
            : base(aLogManager,
                   $"Lovense Device ({friendlyNames[aInterface.Name]})",
                   aInterface,
                   aInfo)
        {
            MsgFuncs.Add(typeof(SingleMotorVibrateCmd), new ButtplugDeviceWrapper(HandleSingleMotorVibrateCmd));
            MsgFuncs.Add(typeof(VibrateCmd), new ButtplugDeviceWrapper(HandleVibrateCmd, new MessageAttributes() { FeatureCount = 1 }));
            MsgFuncs.Add(typeof(StopDeviceCmd), new ButtplugDeviceWrapper(HandleStopDeviceCmd));

            if (friendlyNames[aInterface.Name] == "Nora")
            {
                MsgFuncs.Add(typeof(RotateCmd), new ButtplugDeviceWrapper(HandleRotateCmd, new MessageAttributes() { FeatureCount = 1 }));
            }
        }

        private async Task<ButtplugMessage> HandleStopDeviceCmd(ButtplugDeviceMessage aMsg)
        {
            BpLogger.Debug("Stopping Device " + Name);

            if (friendlyNames[Interface.Name] == "Nora")
            {
                await Interface.WriteValue(aMsg.Id,
                    Info.Characteristics[(uint)LovenseRev1BluetoothInfo.Chrs.Tx],
                    Encoding.ASCII.GetBytes($"Rotate:0;"));
            }

            return await HandleSingleMotorVibrateCmd(new SingleMotorVibrateCmd(aMsg.DeviceIndex, 0, aMsg.Id));
        }

        private async Task<ButtplugMessage> HandleSingleMotorVibrateCmd(ButtplugDeviceMessage aMsg)
        {
            var cmdMsg = aMsg as SingleMotorVibrateCmd;
            if (cmdMsg is null)
            {
                return BpLogger.LogErrorMsg(aMsg.Id, Error.ErrorClass.ERROR_DEVICE, "Wrong Handler");
            }

            return await HandleVibrateCmd(new VibrateCmd(cmdMsg.DeviceIndex,
                new List<VibrateCmd.VibrateSubcommand>() { new VibrateCmd.VibrateSubcommand(0, cmdMsg.Speed) },
                cmdMsg.Id));
        }

        private async Task<ButtplugMessage> HandleVibrateCmd(ButtplugDeviceMessage aMsg)
        {
            var cmdMsg = aMsg as VibrateCmd;
            if (cmdMsg is null)
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

                if (v.Speed == _vibratorSpeed)
                {
                    return new Ok(cmdMsg.Id);
                }

                _vibratorSpeed = v.Speed;
            }

            // While there are 3 lovense revs right now, all of the characteristic arrays are the same.
            return await Interface.WriteValue(aMsg.Id,
                Info.Characteristics[(uint)LovenseRev1BluetoothInfo.Chrs.Tx],
                Encoding.ASCII.GetBytes($"Vibrate:{(int)(_vibratorSpeed * 20)};"));
        }

        private async Task<ButtplugMessage> HandleRotateCmd(ButtplugDeviceMessage aMsg)
        {
            var cmdMsg = aMsg as RotateCmd;
            if (cmdMsg is null)
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

                speedChange = _rotateSpeed != vi.Speed;
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
