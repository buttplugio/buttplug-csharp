// <copyright file="MagicMotionProtocol.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;

namespace Buttplug.Devices.Protocols
{
    internal class MagicMotionProtocol : ButtplugDeviceProtocol
    {
        private readonly double[] _vibratorSpeeds = { 0, 0 };

        internal enum MagicMotionProtocolType
        {
            Protocol1,
            Protocol2,
            Protocol3,
        }

        internal struct MagicMotionType
        {
            public string Brand;
            public string Name;
            public uint VibeCount;
            public MagicMotionProtocolType Protocol;
            public byte MaxSpeed;
        }

        internal static readonly Dictionary<string, MagicMotionType> DevInfos =
            new Dictionary<string, MagicMotionType>()
            {
                {
                    "Smart Mini Vibe",
                    new MagicMotionType()
                    {
                        Brand = "MagicMotion",
                        Name = "Smart Mini Vibe",
                        VibeCount = 1,
                        Protocol = MagicMotionProtocolType.Protocol1,
                        MaxSpeed = 0x64,
                    }
                },
                {
                    "Flamingo",
                    new MagicMotionType()
                    {
                        Brand = "MagicMotion",
                        Name = "Flamingo",
                        VibeCount = 1,
                        Protocol = MagicMotionProtocolType.Protocol1,
                        MaxSpeed = 0x64,
                    }
                },
                {
                    "Magic Cell",
                    new MagicMotionType()
                    {
                        // ToDo: has accelerometer
                        Brand = "MagicMotion",
                        Name = "Dante/Candy",
                        VibeCount = 1,
                        Protocol = MagicMotionProtocolType.Protocol1,
                        MaxSpeed = 0x64,
                    }
                },
                {
                    "Eidolon",
                    new MagicMotionType()
                    {
                        Brand = "MagicMotion",
                        Name = "Eidolon",
                        VibeCount = 2,
                        Protocol = MagicMotionProtocolType.Protocol2,
                        MaxSpeed = 0x64,
                    }
                },
                {
                    "Smart Bean",
                    new MagicMotionType()
                    {
                        // ToDo: Master has pressure sensor, Twins does not
                        Brand = "MagicMotion",
                        Name = "Kegel",
                        VibeCount = 1,
                        Protocol = MagicMotionProtocolType.Protocol1,
                        MaxSpeed = 0x64,
                    }
                },
                {
                    "Magic Wand",
                    new MagicMotionType()
                    {
                        // ToDo: Wand has temperature sensor and heater
                        Brand = "MagicMotion",
                        Name = "Wand",
                        VibeCount = 1,
                        Protocol = MagicMotionProtocolType.Protocol1,
                        MaxSpeed = 0x64,
                    }
                },
                {
                    "Krush",
                    new MagicMotionType()
                    {
                        // ToDo: Receive squeeze sensor packets & capture exact motor values
                        Brand = "LoveLife",
                        Name = "Krush",
                        VibeCount = 1,
                        Protocol = MagicMotionProtocolType.Protocol3,
                        MaxSpeed = 0x4d,
                    }
                },
            };

        private readonly MagicMotionType _devInfo;

        public MagicMotionProtocol(IButtplugLogManager aLogManager,
                           IButtplugDeviceImpl aInterface)
            : base(aLogManager,
                   $"Unknown MagicMotion Device ({aInterface.Name})",
                   aInterface)
        {
            if (DevInfos.ContainsKey(aInterface.Name))
            {
                _devInfo = DevInfos[aInterface.Name];
                Name = $"{_devInfo.Brand} {_devInfo.Name}";
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
            case MagicMotionProtocolType.Protocol1:
                data = new byte[] { 0x0b, 0xff, 0x04, 0x0a, 0x32, 0x32, 0x00, 0x04, 0x08, 0x00, 0x64, 0x00 };
                data[9] = Convert.ToByte(_vibratorSpeeds[0] * _devInfo.MaxSpeed);
                break;

            case MagicMotionProtocolType.Protocol2:
                data = new byte[] { 0x10, 0xff, 0x04, 0x0a, 0x32, 0x0a, 0x00, 0x04, 0x08, 0x00, 0x64, 0x00, 0x04, 0x08, 0x00, 0x64, 0x01 };
                data[9] = Convert.ToByte(_vibratorSpeeds[0] * _devInfo.MaxSpeed);

                if (_devInfo.VibeCount >= 2)
                {
                    data[14] = Convert.ToByte(_vibratorSpeeds[1] * _devInfo.MaxSpeed);
                }

                break;

            case MagicMotionProtocolType.Protocol3:
                data = new byte[] { 0x0b, 0xff, 0x04, 0x0a, 0x46, 0x46, 0x00, 0x04, 0x08, 0x00, 0x64, 0x00 };
                data[9] = Convert.ToByte(_vibratorSpeeds[0] * _devInfo.MaxSpeed);
                break;

            default:
                throw new ButtplugDeviceException(BpLogger,
                    "Unknown communication protocol.",
                    cmdMsg.Id);
            }

            return await Interface.WriteValueAsync(aMsg.Id,
                Endpoints.Tx,
                data, false, aToken).ConfigureAwait(false);
        }
    }
}
