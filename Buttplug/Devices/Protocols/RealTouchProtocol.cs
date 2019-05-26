using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;

namespace Buttplug.Devices.Protocols
{
    internal class RealTouchProtocol : ButtplugDeviceProtocol
    {
        private double _currentPosition = 0;

        // Real Touch Component letters are based on the CDK, which was how
        // RealTouch movies were encoded. Not all component designations are
        // valid for all commands. For instance, you can't have a vector command
        // (which we encode in LinearCmd) run on the lube pump.
        private enum RealTouchComponents
        {
            // Top Tread
            T = 0x00,

            // Bottom Tread
            B = 0x10,

            // Both belts? I think? Maybe?
            U = 0x20,

            // Squeeze (Opening)
            S = 0x30,

            // Heat
            H = 0x40,

            // Lube
            L = 0x50,

            // All
            A = 0x60,
        }

        public RealTouchProtocol(IButtplugLogManager aLogManager, IButtplugDeviceImpl aDevice)
            : base(aLogManager, "RealTouch", aDevice)
        {
            // TODO How do we get a product string here, to tell what firmware version we're on?
            // AddMessageHandler<RotateCmd>(HandleRotateCmd, new MessageAttributes() { FeatureCount = 1 });
            AddMessageHandler<StopDeviceCmd>(HandleStopDeviceCmd);
            AddMessageHandler<FleshlightLaunchFW12Cmd>(HandleFleshlightLaunchFW12Cmd);
            AddMessageHandler<LinearCmd>(HandleLinearCmd);
        }

        public override async Task InitializeAsync(CancellationToken aToken)
        {
            var cmd = new byte[65];
            Array.Clear(cmd, 0, 65);
            await Interface.WriteValueAsync(cmd, aToken);
            cmd = await Interface.ReadValueAsync(new ButtplugDeviceReadOptions(), aToken);

            cmd = GetCommandArray(new[] { (byte)0x0a });
            await Interface.WriteValueAsync(cmd, aToken);
            cmd = await Interface.ReadValueAsync(new ButtplugDeviceReadOptions(), aToken);
            cmd = GetCommandArray(new[] { (byte)0x06, (byte)2, (byte)160, (byte)128, (byte)232, (byte)3 });
            //cmd = GetCommandArray(new byte[] { 0x06 0xff, 0xd0, 0x07 });
            await Interface.WriteValueAsync(cmd, aToken);
            cmd = await Interface.ReadValueAsync(new ButtplugDeviceReadOptions(), aToken);
        }

        // All RealTouch commands need to be packed into a 64-byte array.
        private byte[] GetCommandArray(byte[] aCmd)
        {
            var cmd = new byte[65];
            Array.Clear(cmd, 0, cmd.Length);
            aCmd.CopyTo(cmd, 1);
            return cmd;
        }

        private async Task<ButtplugMessage> HandleStopDeviceCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            BpLogger.Debug("Stopping Device " + Name);
            // Stop commands start with 0x01
            var cmd = GetCommandArray(new[] { (byte)0x01, (byte)RealTouchComponents.A });
            await Interface.WriteValueAsync(cmd, aToken);
            return new Ok(aMsg.Id);
            //return await HandleRotateCmd(new RotateCmd(aMsg.DeviceIndex, 0, _clockwise, aMsg.Id), aToken).ConfigureAwait(false);
        }

        private async Task<ButtplugMessage> HandleFleshlightLaunchFW12Cmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckMessageHandler<FleshlightLaunchFW12Cmd>(aMsg);

            // We'll need to figure out the duration of the Fleshlight move, in
            // order to translate to a LinearCmd message.

            var position = (cmdMsg.Position / 99.0);

            var distance = Math.Abs(_currentPosition - position);

            var duration = FleshlightHelper.GetDuration(distance, (cmdMsg.Speed / 99.0));
            var vectors = new List<LinearCmd.VectorSubcommand>();
            vectors.Add(new LinearCmd.VectorSubcommand(0, (uint)duration, position));
            var msg = new LinearCmd(aMsg.DeviceIndex, vectors, aMsg.Id);

            return await HandleLinearCmd(msg, aToken);
        }

        private async Task<ButtplugMessage> HandleLinearCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckMessageHandler<LinearCmd>(aMsg);
            if (cmdMsg.Vectors.Count > 1)
            {
                throw new ButtplugDeviceException(
                    "LinearCmd requires 1 vector for this device.",
                    cmdMsg.Id);
            }

            var command = new byte[11];
            // Vector commands start with 0x02;
            command[0] = 0x02;
            // Since we're simulating linear movement here, we'll move both belts in the same direction.
            command[1] = (byte)RealTouchComponents.U;
            // If we're moving backward, add 0x80 to our direction component.
            if (cmdMsg.Vectors[0].Position < _currentPosition)
            {
                command[1] += 0x80;
            }

            var speed = FleshlightHelper.GetSpeed(Math.Abs(cmdMsg.Vectors[0].Position - _currentPosition),
                cmdMsg.Vectors[0].Duration);
            _currentPosition = cmdMsg.Vectors[0].Position;

            // This is our speed, or "magnitude" in CDK-speak. Right now, we'll
            // use the position differential like the fleshlight. Not super
            // thrilled with this idea, but blame everyone who decided the
            // Fleshlight Launch was the only fucking toy to ever exist.
            //
            // Also, on my RealTouches, if the speed goes below about 40, things
            // basically stop moving. Make sure our speed doesn't dip below that.
            const double speedMin = 40;
            command[2] = (byte)(speedMin + ((0xff - speedMin) * speed));
            Debug.WriteLine((byte)(0xff * speed));

            // This is our duration, in milliseconds, 16-bit, LE across the two bytes.
            command[3] = (byte)(cmdMsg.Vectors[0].Duration & 0xff);
            command[4] = (byte)((cmdMsg.Vectors[0].Duration & 0xff00) >> 0x8);

            // In envelope, Same magnitude/duration as last 3 bytes.
            // g[5] = inMagnitude
            // g[6] = inDuration & 0xff
            // g[7] = (inDuration & 0xff00) >> 0x8

            // Out envelope, Same magnitude/duration as last 3 bytes.
            // g[9] = outMagnitude
            // g[9] = outDuration & 0xff
            // g[10] = (outDuration & 0xff00) >> 0x8

            var cmd = GetCommandArray(command);
            await Interface.WriteValueAsync(cmd, aToken);
            return new Ok(aMsg.Id);
        }
        /*
        private async Task<ButtplugMessage> HandleRotateCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckMessageHandler<RotateCmd>(aMsg);

            if (cmdMsg.Rotations.Count > 2)
            {
                throw new ButtplugDeviceException(
                    "RotateCmd requires 1 vector for this device.",
                    cmdMsg.Id);
            }


            await Interface.WriteValueAsync(data, aToken);
            return new Ok(aMsg.Id);
        }
        */
    }
}
