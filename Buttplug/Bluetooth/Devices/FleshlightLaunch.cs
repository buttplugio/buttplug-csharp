using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Messages;

namespace Buttplug.Bluetooth.Devices
{
    internal class FleshlightLaunchBluetoothInfo : IBluetoothDeviceInfo
    {
        public enum Chrs : uint
        {
            Tx = 0,
            Rx,
            Cmd,
            Battery
        }
        public string[] Names { get; } = { "Launch" };
        public Guid[] Services { get; } = { new Guid("88f80580-0000-01e6-aace-0002a5d5c51b") };

        public Guid[] Characteristics { get; } =
        {
            // tx
            new Guid("88f80581-0000-01e6-aace-0002a5d5c51b"),
            // rx
            new Guid("88f80582-0000-01e6-aace-0002a5d5c51b"),
            // cmd
            new Guid("88f80583-0000-01e6-aace-0002a5d5c51b")
        };

        public IButtplugDevice CreateDevice(
            IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
        {
            return new FleshlightLaunch(aLogManager,
                                        aInterface);
        }
    }

    internal class FleshlightLaunch : ButtplugBluetoothDevice
    {
        public FleshlightLaunch(IButtplugLogManager aLogManager,
                                IBluetoothDeviceInterface aInterface) :
            base(aLogManager,
                 "Fleshlight Launch",
                 aInterface)
        {
            // Setup message function array
            MsgFuncs.Add(typeof(FleshlightLaunchFW12Cmd), HandleFleshlightLaunchRawCmd);
        }

        public override async Task<ButtplugMessage> Initialize()
        {
            BpLogger.Trace($"Initializing {Name}");
            return await Interface.WriteValue(ButtplugConsts.SYSTEM_MSG_ID,
                (uint) FleshlightLaunchBluetoothInfo.Chrs.Cmd,
                new byte[] {0});
        }

        public async Task<ButtplugMessage> HandleFleshlightLaunchRawCmd(ButtplugDeviceMessage aMsg)
        {
            //TODO: Split into Command message and Control message? (Issue #17)
            var cmdMsg = aMsg as FleshlightLaunchFW12Cmd;
            if (cmdMsg is null)
            {
                return BpLogger.LogErrorMsg(aMsg.Id, "Wrong Handler");
            }
            return await Interface.WriteValue(aMsg.Id, 
                (uint)FleshlightLaunchBluetoothInfo.Chrs.Tx,
                new byte[] { (byte)cmdMsg.Position, (byte)cmdMsg.Speed });            
        }
    }
}