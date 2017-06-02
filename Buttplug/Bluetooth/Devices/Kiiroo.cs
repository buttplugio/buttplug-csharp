using System;
using System.Text;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Messages;

namespace Buttplug.Bluetooth.Devices
{
    internal class KiirooBluetoothInfo : IBluetoothDeviceInfo
    {
        public enum Chrs : uint
        {
            Tx = 0,
            Rx
        }
        public string[] Names { get; } = { "ONYX", "PEARL" };
        public Guid[] Services { get; } = { new Guid("49535343-fe7d-4ae5-8fa9-9fafd205e455") };

        public Guid[] Characteristics { get; } =
        {
            // tx
            new Guid("49535343-8841-43f4-a8d4-ecbe34729bb3"),
            // rx
            new Guid("49535343-1e4d-4bd9-ba61-23c647249616")
        };

        public IButtplugDevice CreateDevice(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
        {
            return new Kiiroo(aLogManager, aInterface);
        }
    }

    internal class Kiiroo : ButtplugBluetoothDevice
    {
        public Kiiroo(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface) :
            base(aLogManager,
                $"Kiiroo {aInterface.Name}",
                aInterface)
        {
            MsgFuncs.Add(typeof(KiirooCmd), HandleKiirooRawCmd);
        }

        public async Task<ButtplugMessage> HandleKiirooRawCmd(ButtplugDeviceMessage aMsg)
        {
            var cmdMsg = aMsg as KiirooCmd;
            if (cmdMsg is null)
            {
                return BpLogger.LogErrorMsg(aMsg.Id, "Wrong Handler");
            }
            return await Interface.WriteValue(cmdMsg.Id,
                (uint)KiirooBluetoothInfo.Chrs.Tx,
                Encoding.ASCII.GetBytes($"{cmdMsg.Position},\n"));
        }
    }
}