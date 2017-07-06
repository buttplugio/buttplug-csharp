using System;
using System.Text;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Messages;
using JetBrains.Annotations;
using static Buttplug.Messages.Error;

namespace Buttplug.Bluetooth.Devices
{
    internal class KiirooBluetoothInfo : IBluetoothDeviceInfo
    {
        public enum Chrs : uint
        {
            Rx = 0,
            Tx = 1,
        }

        public string[] Names { get; } = { "ONYX", "PEARL" };

        public Guid[] Services { get; } = { new Guid("49535343-fe7d-4ae5-8fa9-9fafd205e455") };

        public Guid[] Characteristics { get; } =
        {
            // rx
            new Guid("49535343-1e4d-4bd9-ba61-23c647249616"),

            // tx
            new Guid("49535343-8841-43f4-a8d4-ecbe34729bb3"),
        };

        public IButtplugDevice CreateDevice(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
        {
            return new Kiiroo(aLogManager, aInterface);
        }
    }

    internal class Kiiroo : ButtplugBluetoothDevice
    {
        public Kiiroo([NotNull] IButtplugLogManager aLogManager,
                      [NotNull] IBluetoothDeviceInterface aInterface)
            : base(aLogManager,
                   $"Kiiroo {aInterface.Name}",
                   aInterface)
        {
            MsgFuncs.Add(typeof(KiirooCmd), HandleKiirooRawCmd);
            MsgFuncs.Add(typeof(StopDeviceCmd), HandleStopDeviceCmd);
        }

        private Task<ButtplugMessage> HandleStopDeviceCmd([NotNull] ButtplugDeviceMessage aMsg)
        {
            // Right now, this is a nop. The Onyx doesn't have any sort of permanent movement state,
            // and its longest movement is like 150ms or so. The Pearl is supposed to vibrate but I've
            // never gotten that to work. So for now, we just return ok.
            return Task.FromResult<ButtplugMessage>(new Ok(aMsg.Id));
        }

        private async Task<ButtplugMessage> HandleKiirooRawCmd([NotNull] ButtplugDeviceMessage aMsg)
        {
            var cmdMsg = aMsg as KiirooCmd;
            if (cmdMsg is null)
            {
                return BpLogger.LogErrorMsg(aMsg.Id, ErrorClass.ERROR_DEVICE, "Wrong Handler");
            }

            return await Interface.WriteValue(cmdMsg.Id,
                                              (uint)KiirooBluetoothInfo.Chrs.Tx,
                                              Encoding.ASCII.GetBytes($"{cmdMsg.Position},\n"));
        }
    }
}