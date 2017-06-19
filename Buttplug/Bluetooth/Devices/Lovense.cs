using System;
using System.Text;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Messages;
using static Buttplug.Messages.Error;

namespace Buttplug.Bluetooth.Devices
{
    internal class LovenseRev1BluetoothInfo : IBluetoothDeviceInfo
    {
        public enum Chrs : uint
        {
            Tx = 0,
            Rx
        }

        public Guid[] Services { get; } = { new Guid("0000fff0-0000-1000-8000-00805f9b34fb") };
        public string[] Names { get; } = 
        {
            // Nora
            "LVS-A011", "LVS-C011",
            // Max
            "LVS-B011" };

        public Guid[] Characteristics { get; } =
        {
            // tx characteristic
            new Guid("0000fff2-0000-1000-8000-00805f9b34fb")//,
            // rx characteristic
            // Comment out until issue #108 is fixed. Characteristic isn't really needed until Issue #9 is implemented also.
            //new Guid("0000fff1-0000-1000-8000-00805f9b34fb")
        };

        public IButtplugDevice CreateDevice(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
        {
            return new Lovense(aLogManager,
                aInterface);
        }
    }

    internal class LovenseRev2BluetoothInfo : IBluetoothDeviceInfo
    {
        public enum Chrs : uint
        {
            Tx = 0,
            Rx
        }

        public Guid[] Services { get; } = { new Guid("6e400001-b5a3-f393-e0a9-e50e24dcca9e") };
        public string[] Names { get; } =
        {
            // Lush
            "LVS-S001",
            // Hush
            "LVS-Z001"
        };

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

    internal class LovenseRev3BluetoothInfo : IBluetoothDeviceInfo
    {
        public enum Chrs : uint
        {
            Tx = 0,
            Rx
        }

        public Guid[] Services { get; } = { new Guid("50300001-0024-4bd4-bbd5-a6920e4c5653") };
        public string[] Names { get; } =
        {
            // Edge
            "LVS-P36"
        };

        public Guid[] Characteristics { get; } =
        {
            // tx characteristic
            new Guid("50300002-0024-4bd4-bbd5-a6920e4c5653"),
            // rx characteristic
            new Guid("50300003-0024-4bd4-bbd5-a6920e4c5653")
        };

        public IButtplugDevice CreateDevice(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
        {
            return new Lovense(aLogManager,
                aInterface);
        }
    }

    internal class Lovense : ButtplugBluetoothDevice
    {
        public Lovense(IButtplugLogManager aLogManager,
                       IBluetoothDeviceInterface aInterface) :
            base(aLogManager,
                 $"Lovense Device ({aInterface.Name})",
                 aInterface)
        {
            MsgFuncs.Add(typeof(SingleMotorVibrateCmd), HandleSingleMotorVibrateCmd);
            MsgFuncs.Add(typeof(StopDeviceCmd), HandleStopDeviceCmd);
        }

        private async Task<ButtplugMessage> HandleStopDeviceCmd(ButtplugDeviceMessage aMsg)
        {
            return await HandleSingleMotorVibrateCmd(new SingleMotorVibrateCmd(aMsg.DeviceIndex, 0, aMsg.Id));
        }

        private async Task<ButtplugMessage> HandleSingleMotorVibrateCmd(ButtplugDeviceMessage aMsg)
        {
            var cmdMsg = aMsg as SingleMotorVibrateCmd;
            if (cmdMsg is null)
            {
                return BpLogger.LogErrorMsg(aMsg.Id, ErrorClass.ERROR_DEVICE, "Wrong Handler");
            }
            return await Interface.WriteValue(aMsg.Id, 
                // While there are 3 lovense revs right now, all of the characteristic arrays are the same.
                (uint)LovenseRev1BluetoothInfo.Chrs.Tx,
                Encoding.ASCII.GetBytes($"Vibrate:{(int)(cmdMsg.Speed * 20)};"));
        }
    }
}
