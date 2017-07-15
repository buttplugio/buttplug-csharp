using System;
using System.Text;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;

namespace Buttplug.Server.Bluetooth.Devices
{
    internal class VibratissimoBluetoothInfo : IBluetoothDeviceInfo
    {
        public enum Chrs : uint
        {
            TxMode = 0,
            TxSpeed,
            Rx,
        }

        public Guid[] Services { get; } = { new Guid("00001523-1212-efde-1523-785feabcd123") };

        // Deliberately empty: you can rename these!
        public string[] Names { get; } =
        {
        };

        public Guid[] Characteristics { get; } =
        {
            // tx characteristic
            new Guid("00001524-1212-efde-1523-785feabcd123"),
            new Guid("00001526-1212-efde-1523-785feabcd123"),

            // rx characteristic
            new Guid("00001527-1212-efde-1523-785feabcd123"),
        };

        public IButtplugDevice CreateDevice(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
        {
            return new Vibratissimo(aLogManager,
                aInterface);
        }
    }

    internal class Vibratissimo : ButtplugBluetoothDevice
    {
        public Vibratissimo(IButtplugLogManager aLogManager,
                       IBluetoothDeviceInterface aInterface)
            : base(aLogManager,
                   $"Vibratissimo Device ({aInterface.Name})",
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
                return BpLogger.LogErrorMsg(aMsg.Id, Error.ErrorClass.ERROR_DEVICE, "Wrong Handler");
            }

            var data = new byte[2];
            data[0] = 0x03;
            data[1] = 0xFF;
            await Interface.WriteValue(aMsg.Id, (uint)VibratissimoBluetoothInfo.Chrs.TxMode, data);

            data[0] = Convert.ToByte(cmdMsg.Speed * byte.MaxValue);
            data[1] = 0x00;
            return await Interface.WriteValue(aMsg.Id, (uint)VibratissimoBluetoothInfo.Chrs.TxSpeed, data);
        }
    }
}
