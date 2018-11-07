// <copyright file="MagicMotion.cs" company="Nonpolynomial Labs LLC">
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

namespace Buttplug.Server.Bluetooth.Devices
{
    internal class MagicMotionBluetoothInfo : IBluetoothDeviceInfo
    {
        public enum Chrs : uint
        {
            Tx = 0,
        }

        /*
         * ToDo: Rx on other service
         * Service UUID: 6f468792-f91f-11e3-a847-b2227cce2b54
         * Char UUID: 6f468bfc-f91f-11e3-a847-b2227cce2b54
         */

        public Guid[] Services { get; } = { new Guid("78667579-7b48-43db-b8c5-7928a6b0a335") };

        public string[] NamePrefixes { get; } = { };

        public string[] Names { get; } =
        {
            "Smart Mini Vibe",
            "Flamingo",
            "Eidolon",
            "Smart Bean", // Kegel Twins/Master
            "Magic Cell", // Dante/Candy
            "Magic Wand",
        };

        public Dictionary<uint, Guid> Characteristics { get; } = new Dictionary<uint, Guid>()
        {
            // tx characteristic
            { (uint)Chrs.Tx, new Guid("78667579-a914-49a4-8333-aa3c0cd8fedc") },
        };

        public IButtplugDevice CreateDevice(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
        {
            return new MagicMotion(aLogManager, aInterface, this);
        }
    }

    internal class MagicMotion : ButtplugBluetoothDevice
    {
        private readonly double[] _vibratorSpeeds = { 0, 0 };

        internal enum MagicMotionProtocol
        {
            Protocol1,
            Protocol2,
        }

        internal struct MagicMotionType
        {
            public string Name;
            public uint VibeCount;
            public MagicMotionProtocol Protocol;
        }

        internal static readonly Dictionary<string, MagicMotionType> DevInfos =
            new Dictionary<string, MagicMotionType>()
            {
                {
                    "Smart Mini Vibe",
                    new MagicMotionType()
                    {
                        Name = "Smart Mini Vibe",
                        VibeCount = 1,
                        Protocol = MagicMotionProtocol.Protocol1,
                    }
                },
                {
                    "Flamingo",
                    new MagicMotionType()
                    {
                        Name = "Flamingo",
                        VibeCount = 1,
                        Protocol = MagicMotionProtocol.Protocol1,
                    }
                },
                {
                    "Magic Cell",
                    new MagicMotionType()
                    {
                        // ToDo: has accelerometer
                        Name = "Dante/Candy",
                        VibeCount = 1,
                        Protocol = MagicMotionProtocol.Protocol1,
                    }
                },
                {
                    "Eidolon",
                    new MagicMotionType()
                    {
                        Name = "Eidolon",
                        VibeCount = 2,
                        Protocol = MagicMotionProtocol.Protocol2,
                    }
                },
                {
                    "Smart Bean",
                    new MagicMotionType()
                    {
                        // ToDo: Master has pressure sensor, Twins does not
                        Name = "Kegel",
                        VibeCount = 1,
                        Protocol = MagicMotionProtocol.Protocol1,
                    }
                },
                {
                    "Magic Wand",
                    new MagicMotionType()
                    {
                        // ToDo: Wand has temperature sensor and heater
                        Name = "Wand",
                        VibeCount = 2,
                        Protocol = MagicMotionProtocol.Protocol1,
                    }
                },
            };

        private readonly MagicMotionType _devInfo;

        public MagicMotion(IButtplugLogManager aLogManager,
                           IBluetoothDeviceInterface aInterface,
                           IBluetoothDeviceInfo aInfo)
            : base(aLogManager,
                   $"Unknown MagicMotion Device ({aInterface.Name})",
                   aInterface,
                   aInfo)
        {
            if (DevInfos.ContainsKey(aInterface.Name))
            {
                Name = $"MagicMotion {DevInfos[aInterface.Name].Name}";
                _devInfo = DevInfos[aInterface.Name];
            }
            else
            {
                BpLogger.Warn($"Cannot identify device {Name}, defaulting to Smart Mini Vibe settings.");
                _devInfo = DevInfos["Smart Mini Vibe"];
            }

            AddMessageHandler<SingleMotorVibrateCmd>(HandleSingleMotorVibrateCmd);
            AddMessageHandler<VibrateCmd>(HandleVibrateCmd, new MessageAttributes { FeatureCount = _devInfo.VibeCount });
            AddMessageHandler<StopDeviceCmd>(HandleStopDeviceCmd);
        }

        private async Task<ButtplugMessage> HandleStopDeviceCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            return await HandleSingleMotorVibrateCmd(new SingleMotorVibrateCmd(aMsg.DeviceIndex, 0, aMsg.Id), aToken).ConfigureAwait(false);
        }

        private async Task<ButtplugMessage> HandleSingleMotorVibrateCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckMessageHandler<SingleMotorVibrateCmd>(aMsg);

            return await HandleVibrateCmd(VibrateCmd.Create(cmdMsg.DeviceIndex, cmdMsg.Id, cmdMsg.Speed, _devInfo.VibeCount), aToken).ConfigureAwait(false);
        }

        private async Task<ButtplugMessage> HandleVibrateCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckGenericMessageHandler<VibrateCmd>(aMsg, _devInfo.VibeCount);

            var changed = false;

            foreach (var v in cmdMsg.Speeds)
            {
                if (Math.Abs(v.Speed - _vibratorSpeeds[v.Index]) < 0.001)
                {
                    continue;
                }

                changed = true;
                _vibratorSpeeds[v.Index] = v.Speed;
            }

            if (!changed)
            {
                return new Ok(cmdMsg.Id);
            }

            byte[] data;
            switch (_devInfo.Protocol)
            {
            case MagicMotionProtocol.Protocol1:
                data = new byte[] { 0x0b, 0xff, 0x04, 0x0a, 0x32, 0x32, 0x00, 0x04, 0x08, 0x00, 0x64, 0x00 };
                data[9] = Convert.ToByte(_vibratorSpeeds[0] * byte.MaxValue);
                break;

            case MagicMotionProtocol.Protocol2:
                data = new byte[] { 0x10, 0xff, 0x04, 0x0a, 0x32, 0x0a, 0x00, 0x04, 0x08, 0x00, 0x64, 0x00, 0x04, 0x08, 0x00, 0x64, 0x01 };
                data[9] = Convert.ToByte(_vibratorSpeeds[0] * byte.MaxValue);

                if (_devInfo.VibeCount >= 2)
                {
                    data[14] = Convert.ToByte(_vibratorSpeeds[1] * byte.MaxValue);
                }

                break;

            default:
                throw new ButtplugDeviceException(BpLogger,
                    "Unknown communication protocol.",
                    cmdMsg.Id);
            }

            return await Interface.WriteValueAsync(aMsg.Id,
                (uint)MagicMotionBluetoothInfo.Chrs.Tx,
                data, false, aToken).ConfigureAwait(false);
        }
    }
}
