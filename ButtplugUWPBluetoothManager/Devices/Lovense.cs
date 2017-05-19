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
    internal class LovenseBluetoothInfo : IBluetoothDeviceInfo
    {
        public enum Chrs : uint
        {
            Tx = 0,
            Rx
        }

        public Guid[] Services { get; } = { new Guid("6e400001-b5a3-f393-e0a9-e50e24dcca9e") };
        public string[] Names { get; } = { "LVS-S001", "LVS-Z001" };

        public Guid[] Characteristics { get; } =
        {
            // tx characteristic
            new Guid("6e400002-b5a3-f393-e0a9-e50e24dcca9e"),
            // rx characteristic
            new Guid("6e400003-b5a3-f393-e0a9-e50e24dcca9e")
        };

        public IButtplugDevice CreateDevice(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
        {
            return new Lovense(aLogManager,
                               aInterface);
        }
    }

    internal class Lovense : ButtplugDevice
    {
        private IBluetoothDeviceInterface _interface;

        public Lovense(IButtplugLogManager aLogManager,
                       IBluetoothDeviceInterface aInterface) :
            base(aLogManager,
                 $"Lovense Device ({aInterface.Name})")
        {
            _interface = aInterface;
            MsgFuncs.Add(typeof(SingleMotorVibrateCmd), HandleSingleMotorVibrateCmd);
        }

        public async Task<ButtplugMessage> HandleSingleMotorVibrateCmd(ButtplugDeviceMessage aMsg)
        {
            var cmdMsg = aMsg as SingleMotorVibrateCmd;
            if (cmdMsg is null)
            {
                return BpLogger.LogErrorMsg(aMsg.Id, "Wrong Handler");
            }
            var buf = ButtplugBluetoothUtils.WriteString($"Vibrate:{(int)(cmdMsg.Speed * 20)};");
            return await _interface.WriteValue(aMsg.Id, 
                (uint)LovenseBluetoothInfo.Chrs.Tx,
                Encoding.ASCII.GetBytes($"Vibrate:{(int)(cmdMsg.Speed * 20)};"));
        }

        public override async Task<ButtplugMessage> Initialize()
        {
            return new Ok(ButtplugConsts.SYSTEM_MSG_ID);
        }
    }
}