// <copyright file="Youou.cs" company="Nonpolynomial Labs LLC">
//     Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
//     Copyright (c) Nonpolynomial Labs LLC. All rights reserved. Licensed under the BSD 3-Clause
//     license. See LICENSE file in the project root for full license information.
// </copyright>

using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Buttplug.Devices.Protocols
{
    internal class YououProtocol : ButtplugDeviceProtocol
    {
        private double _vibratorSpeed;
        private byte _packetId = 0;

        public YououProtocol(IButtplugLogManager aLogManager,
                             IButtplugDeviceImpl aInterface)
            : base(aLogManager,
                   "Youou Wand Vibrator",
                   aInterface)
        {
            AddMessageHandler<SingleMotorVibrateCmd>(HandleSingleMotorVibrateCmd);
            AddMessageHandler<VibrateCmd>(HandleVibrateCmd, new MessageAttributes() { FeatureCount = 1 });
            AddMessageHandler<StopDeviceCmd>(HandleStopDeviceCmd);
        }

        private async Task<ButtplugMessage> HandleStopDeviceCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            BpLogger.Debug("Stopping Device " + Name);
            return await HandleSingleMotorVibrateCmd(new SingleMotorVibrateCmd(aMsg.DeviceIndex, 0, aMsg.Id), aToken).ConfigureAwait(false);
        }

        private async Task<ButtplugMessage> HandleSingleMotorVibrateCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckMessageHandler<SingleMotorVibrateCmd>(aMsg);

            return await HandleVibrateCmd(VibrateCmd.Create(cmdMsg.DeviceIndex, cmdMsg.Id, cmdMsg.Speed, 1), aToken).ConfigureAwait(false);
        }

        private async Task<ButtplugMessage> HandleVibrateCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckGenericMessageHandler<VibrateCmd>(aMsg, 1);
            var v = cmdMsg.Speeds[0];

            if (Math.Abs(v.Speed - _vibratorSpeed) < 0.001 && SentVibration)
            {
                return new Ok(cmdMsg.Id);
            }

            SentVibration = true;

            // Byte 2 seems to be a monotonically increasing packet id of some kind Speed seems to be
            // 0-247 or so. Anything above that sets a pattern which isn't what we want here.
            var maxValue = (byte)247;
            var speed = (byte)(cmdMsg.Speeds[0].Speed * maxValue);
            var state = (byte)(cmdMsg.Speeds[0].Speed > 0.001 ? 1 : 0);
            var cmdData = new byte[] { 0xaa, 0x55, _packetId, 0x02, 0x03, 0x01, speed, state };
            byte crc = 0;

            // Simple XOR of everything up to the 9th byte for CRC.
            foreach (var b in cmdData)
            {
                crc = (byte)(b ^ crc);
            }

            var data = cmdData.Concat(new byte[] { crc, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });

            this._packetId += 1;
            if (this._packetId > 255)
            {
                this._packetId = 0;
            }

            await Interface.WriteValueAsync(data.ToArray(), aToken).ConfigureAwait(false);
            return new Ok(aMsg.Id);
        }
    }
}
