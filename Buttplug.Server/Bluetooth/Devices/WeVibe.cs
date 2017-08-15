using System;
using System.Text;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;

namespace Buttplug.Server.Bluetooth.Devices
{
    internal class WeVibeBluetoothInfo : IBluetoothDeviceInfo
    {
        public enum Chrs : uint
        {
            Tx = 0,
            Rx,
        }

        public Guid[] Services { get; } = { new Guid("f000bb03-0451-4000-b000-000000000000") };

        public string[] Names { get; } =
        {
            "Pivot",
            "Verge",
        };

        public Guid[] Characteristics { get; } =
        {
            // tx characteristic
            new Guid("f000c000-0451-4000-b000-000000000000"),

            // rx characteristic
            new Guid("f000b000-0451-4000-b000-000000000000"),
        };

        public IButtplugDevice CreateDevice(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
        {
            return new WeVibe(aLogManager, aInterface, this);
        }
    }

    internal class WeVibe : ButtplugBluetoothDevice
    {
        public WeVibe(IButtplugLogManager aLogManager,
                      IBluetoothDeviceInterface aInterface,
                      IBluetoothDeviceInfo aInfo)
            : base(aLogManager,
                   $"WeVibe Device ({aInterface.Name})",
                   aInterface,
                   aInfo)
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

            var rSpeed = Convert.ToUInt16(cmdMsg.Speed * 15);

            // 0f 03 00 bc 00 00 00 00
            var data = new byte[] { 0x0f, 0x03, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00 };
            data[3] = Convert.ToByte(rSpeed); // External
            data[3] |= Convert.ToByte(rSpeed << 4); // Internal

            if (rSpeed == 0)
            {
                data[1] = 0x00;
                data[5] = 0x00;
            }

            return await Interface.WriteValue(aMsg.Id,
                Info.Characteristics[(uint)WeVibeBluetoothInfo.Chrs.Tx],
                data);
        }
    }
}
