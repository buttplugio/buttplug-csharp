using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace Buttplug.Devices.Protocols
{
    class NikkuProtocol : ButtplugDeviceProtocol
    {
        private double _lastPosition;

        public NikkuProtocol([NotNull] IButtplugLogManager aLogManager,
            IButtplugDeviceImpl aInterface)
            : base(aLogManager,
                "Nikku Device",
                aInterface)
        {
            // Setup message function array
            AddMessageHandler<FleshlightLaunchFW12Cmd>(HandleFleshlightLaunchFW12Cmd);
            AddMessageHandler<LinearCmd>(HandleLinearCmd, new MessageAttributes() { FeatureCount = 1 });
            AddMessageHandler<StopDeviceCmd>(HandleStopDeviceCmd);
        }

        public override async Task InitializeAsync(CancellationToken aToken)
        {
        }

        private Task<ButtplugMessage> HandleStopDeviceCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            // This probably shouldn't be a nop, but right now we don't have a good way to know
            // if the launch is moving or not, and surprisingly enough, setting speed to 0 does not
            // actually stop movement. It just makes it move really slow.
            // However, since each move it makes is finite (unlike setting vibration on some devices),
            // so we can assume it will be a short move, similar to what we do for the Kiiroo toys.
            BpLogger.Debug("Stopping Device " + Name);
            return Task.FromResult<ButtplugMessage>(new Ok(aMsg.Id));
        }

        private async Task<ButtplugMessage> HandleLinearCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckGenericMessageHandler<LinearCmd>(aMsg, 1);
            var v = cmdMsg.Vectors[0];

            var commandStr = $"{(int)(v.Position * 100)},{v.Duration}\n";

            await Interface.WriteValueAsync(Encoding.ASCII.GetBytes(commandStr),
                aToken).ConfigureAwait(false);
            return new Ok(aMsg.Id);
        }

        private async Task<ButtplugMessage> HandleFleshlightLaunchFW12Cmd(ButtplugDeviceMessage aMsg,
            CancellationToken aToken)
        {
            var cmdMsg = CheckMessageHandler<FleshlightLaunchFW12Cmd>(aMsg);
            var duration = FleshlightHelper.GetDuration(Math.Abs(_lastPosition - cmdMsg.Position), cmdMsg.Speed);
            _lastPosition = Convert.ToDouble(cmdMsg.Position) / 99;

            var commandStr = $"{cmdMsg.Position},{duration}\n";

            await Interface.WriteValueAsync(Encoding.ASCII.GetBytes(commandStr),
                aToken).ConfigureAwait(false);
            return new Ok(aMsg.Id);
        }
    }
}
