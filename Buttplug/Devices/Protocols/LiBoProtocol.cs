// <copyright file="LiBoProtocol.cs" company="Nonpolynomial Labs LLC">
//     Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
//     Copyright (c) Nonpolynomial Labs LLC. All rights reserved. Licensed under the BSD 3-Clause
//     license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;

namespace Buttplug.Devices.Protocols
{
    internal class LiBoProtocol : ButtplugDeviceProtocol
    {
        internal class LiBoType
        {
            public string Name;
            public uint VibeCount;
            public uint[] VibeOrder = new uint[] { 0, 1 };
            public bool MultiCharacteristic = true;
        }

        internal static readonly Dictionary<string, LiBoType> DevInfos =
            new Dictionary<string, LiBoType>()
            {
                {
                    "PiPiJing",
                    new LiBoType()
                    {
                        Name = "Elle",
                        VibeCount = 2, // Shock as vibe
                        VibeOrder = new uint[] { 1, 0 },
                    }
                },
                {
                    "XiaoLu",
                    new LiBoType()
                    {
                        Name = "Lottie",
                        VibeCount = 1,
                    }
                },
                {
                    "SuoYinQiu",
                    new LiBoType()
                    {
                        Name = "Karen",
                        VibeCount = 0,
                    }
                },
                {
                    "BaiHu",
                    new LiBoType()
                    {
                        Name = "LaLa",
                        VibeCount = 2, // Suction as vibe
                    }
                },
                {
                    "LuXiaoHan",
                    new LiBoType()
                    {
                        Name = "LuLu",
                        VibeCount = 1,
                    }
                },
                {
                    "MonsterPub",
                    new LiBoType()
                    {
                        Name = "MonsterPub",
                        VibeCount = 1,
                    }
                },
                {
                    "Gugudai",
                    new LiBoType()
                    {
                        Name = "Carlos",
                        VibeCount = 2, // Suction as vibe
                    }
                },
                {
                    "ShaYu",
                    new LiBoType()
                    {
                        Name = "Shark",
                        VibeCount = 2,
                        MultiCharacteristic = false,
                    }
                },
                {
                    "Yuyi",
                    new LiBoType()
                    {
                        Name = "Lina",
                        VibeCount = 1,
                    }
                },
                {
                    "LuWuShuang",
                    new LiBoType()
                    {
                        Name = "Adel",
                        VibeCount = 1,
                    }
                },
                {
                    "LiBo",
                    new LiBoType()
                    {
                        Name = "Lily",
                        VibeCount = 2,
                    }
                },
            };

        private readonly LiBoType _devInfo;
        private readonly double[] _vibratorSpeed = { 0, 0 };

        public LiBoProtocol(IButtplugLogManager aLogManager,
                      IButtplugDeviceImpl aInterface)
            : base(aLogManager,
                   $"LiBo ({aInterface.Name})",
                   aInterface)
        {
            if (DevInfos.ContainsKey(aInterface.Name))
            {
                _devInfo = DevInfos[aInterface.Name];
                Name = $"LiBo {_devInfo.Name}";
            }
            else
            {
                // Pick the single vibe baseline
                BpLogger.Warn($"Cannot identify device {Name}, defaulting to LuLu settings.");
                _devInfo = DevInfos["LuXiaoHan"];
            }

            AddMessageHandler<StopDeviceCmd>(HandleStopDeviceCmd);
            if (_devInfo.VibeCount > 0)
            {
                AddMessageHandler<SingleMotorVibrateCmd>(HandleSingleMotorVibrateCmd);
                AddMessageHandler<VibrateCmd>(HandleVibrateCmd,
                    new MessageAttributes { FeatureCount = _devInfo.VibeCount });
            }

            // TODO Add an explicit handler for Estim shocking, kegel pressure and add a battery handler.
        }

        private async Task<ButtplugMessage> HandleStopDeviceCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            // We can't actually stop the Karen

            if (_devInfo.VibeCount > 0)
            {
                return await HandleSingleMotorVibrateCmd(new SingleMotorVibrateCmd(aMsg.DeviceIndex, 0, aMsg.Id),
                    aToken).ConfigureAwait(false);
            }

            return new Ok(aMsg.Id);
        }

        private async Task<ButtplugMessage> HandleSingleMotorVibrateCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckMessageHandler<SingleMotorVibrateCmd>(aMsg);

            return await HandleVibrateCmd(VibrateCmd.Create(cmdMsg.DeviceIndex, cmdMsg.Id, cmdMsg.Speed, _devInfo.VibeCount), aToken).ConfigureAwait(false);
        }

        private async Task<ButtplugMessage> HandleVibrateCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckGenericMessageHandler<VibrateCmd>(aMsg, _devInfo.VibeCount);

            var changed = new[] { false, false };

            foreach (var v in cmdMsg.Speeds)
            {
                if (!(Math.Abs(v.Speed - _vibratorSpeed[v.Index]) > 0.001))
                {
                    continue;
                }

                changed[v.Index] = true;
                _vibratorSpeed[v.Index] = v.Speed;
            }

            // Shark specific handling
            if (_devInfo.VibeCount == 2 && !_devInfo.MultiCharacteristic)
            {
                if (!changed.Select(x => x).Any() && SentVibration)
                {
                    return new Ok(aMsg.Id);
                }

                SentVibration = true;
                byte data = 0x00;
                data |= (byte)((byte)Math.Ceiling(_vibratorSpeed[_devInfo.VibeOrder[0]] * 0x03) << 4);
                data |= (byte)Math.Ceiling(_vibratorSpeed[_devInfo.VibeOrder[1]] * 0x03);
                await Interface.WriteValueAsync(
                    new[] { data },
                    new ButtplugDeviceWriteOptions { Endpoint = Endpoints.Tx },
                    aToken).ConfigureAwait(false);

                return new Ok(aMsg.Id);
            }

            if (changed[_devInfo.VibeOrder[0]] || !SentVibration)
            {
                // Map a 0 - 100% value to a 0 - 0x64 value since 0 * x == 0 this will turn off the vibe if
                // speed is 0.00
                await Interface.WriteValueAsync(
                    new[] { Convert.ToByte((uint)Math.Ceiling(_vibratorSpeed[_devInfo.VibeOrder[0]] * 0x64)) },
                    new ButtplugDeviceWriteOptions { Endpoint = Endpoints.Tx },
                    aToken).ConfigureAwait(false);

                // Some devices don't really stop unless both characteristics are set to 0
                if (_devInfo.VibeCount == 1 && (uint)Math.Ceiling(_vibratorSpeed[_devInfo.VibeOrder[0]] * 0x64) == 0)
                {
                    await Interface.WriteValueAsync(
                        new[] { Convert.ToByte((uint)Math.Ceiling(_vibratorSpeed[_devInfo.VibeOrder[0]] * 0x64)) },
                        new ButtplugDeviceWriteOptions { Endpoint = Endpoints.TxMode },
                        aToken).ConfigureAwait(false);
                }
            }

            if (_devInfo.VibeCount < 2 ||
                (!changed[_devInfo.VibeOrder[1]] && SentVibration) )
            {
                if (_devInfo.VibeCount < 2)
                {
                    SentVibration = true;
                }

                return new Ok(aMsg.Id);
            }

            SentVibration = true;

            // Map a 0 - 100% value to a 0 - 3 value since 0 * x == 0 this will turn off the vibe if
            // speed is 0.00
            await Interface.WriteValueAsync(
                new[] { Convert.ToByte((uint)Math.Ceiling(_vibratorSpeed[_devInfo.VibeOrder[1]] * 3)) },
                new ButtplugDeviceWriteOptions { Endpoint = Endpoints.TxMode },
                aToken).ConfigureAwait(false);

            return new Ok(aMsg.Id);
        }
    }
}
