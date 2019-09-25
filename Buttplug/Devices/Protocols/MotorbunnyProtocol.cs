using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;

namespace Buttplug.Devices.Protocols
{
    internal class MotorbunnyProtocol : ButtplugDeviceProtocol
    {
        private double _vibratorSpeed;

        public MotorbunnyProtocol(IButtplugLogManager aLogManager,
                                  IButtplugDeviceImpl aInterface)
            : base(aLogManager,
                   "Motorbunny",
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

            // If speed is zero, send the magic stop packet, otherwise *weird* things happen.
            if ((cmdMsg.Speeds[0].Speed * 255) < 5)
            {
                await Interface.WriteValueAsync(new byte[] { 0xf0, 0x00, 0x00, 0x00, 0x00, 0xec }, aToken).ConfigureAwait(false);
                return new Ok(aMsg.Id);
            }


            var cmdData = new byte[] { 0xff };

            for (var i = 0; i < 7; ++i)
            {
                cmdData = cmdData.Concat(new byte[] { (byte)(cmdMsg.Speeds[0].Speed * 255), 0x14 }).ToArray();
            }

            byte crc = 0;

            // Skip the first byte for the CRC
            for (var j = 1; j < cmdData.Length; ++j)
            {
                crc = (byte)(cmdData[j] + crc);
            }

            cmdData = cmdData.Concat(new byte[] { crc, 0xec }).ToArray();

            await Interface.WriteValueAsync(cmdData, aToken).ConfigureAwait(false);
            return new Ok(aMsg.Id);
        }
    }
}
