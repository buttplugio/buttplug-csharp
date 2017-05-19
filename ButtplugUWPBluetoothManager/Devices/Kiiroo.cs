using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Buttplug.Core;
using Buttplug.Messages;
using ButtplugUWPBluetoothManager.Core;

namespace Buttplug.Devices
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

    internal class Kiiroo : ButtplugDevice
    {
        private IBluetoothDeviceInterface _interface;

        public Kiiroo(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface) :
            base(aLogManager,
                $"Kiiroo {aInterface.Name}"
                 )
        {
            _interface = aInterface;
            MsgFuncs.Add(typeof(KiirooRawCmd), HandleKiirooRawCmd);
        }

        public async Task<ButtplugMessage> HandleKiirooRawCmd(ButtplugDeviceMessage aMsg)
        {
            var cmdMsg = aMsg as KiirooRawCmd;
            if (cmdMsg is null)
            {
                return BpLogger.LogErrorMsg(aMsg.Id, "Wrong Handler");
            }
            return await _interface.WriteValue(cmdMsg.Id,
                (uint)KiirooBluetoothInfo.Chrs.Tx,
                Encoding.ASCII.GetBytes($"{cmdMsg.Position},\n"));
        }

        public override async Task<ButtplugMessage> Initialize()
        {
            return new Ok(ButtplugConsts.SYSTEM_MSG_ID);
        }
    }
}