using System;
using System.Text;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;
using System.Collections.Generic;

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
            "4 Plus",
            "Ditto",
            "Nova",
            "Pivot",
            "Wish",
            "Verge",
            "Cougar",
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
        private double _vibratorSpeed = 0;

        public WeVibe(IButtplugLogManager aLogManager,
                      IBluetoothDeviceInterface aInterface,
                      IBluetoothDeviceInfo aInfo)
            : base(aLogManager,
                   $"WeVibe Device ({aInterface.Name})",
                   aInterface,
                   aInfo)
        {
            MsgFuncs.Add(typeof(SingleMotorVibrateCmd), new ButtplugDeviceWrapper(HandleSingleMotorVibrateCmd));
            MsgFuncs.Add(typeof(VibrateCmd), new ButtplugDeviceWrapper(HandleVibrateCmd, new MessageAttributes() { FeatureCount = 1 }));
            MsgFuncs.Add(typeof(StopDeviceCmd), new ButtplugDeviceWrapper(HandleStopDeviceCmd));
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

            return await HandleVibrateCmd(new VibrateCmd(cmdMsg.DeviceIndex,
                new List<VibrateCmd.VibrateIndex>() { new VibrateCmd.VibrateIndex(0, cmdMsg.Speed) },
                cmdMsg.Id));
        }

        private async Task<ButtplugMessage> HandleVibrateCmd(ButtplugDeviceMessage aMsg)
        {
            var cmdMsg = aMsg as VibrateCmd;
            if (cmdMsg is null)
            {
                return BpLogger.LogErrorMsg(aMsg.Id, Error.ErrorClass.ERROR_DEVICE, "Wrong Handler");
            }

            foreach (var vi in cmdMsg.Speeds)
            {
                if (vi.Index == 0)
                {
                    if (vi.Speed == _vibratorSpeed)
                    {
                        return new Ok(cmdMsg.Id);
                    }

                    _vibratorSpeed = vi.Speed;
                }
            }

            var rSpeed = Convert.ToUInt16(_vibratorSpeed * 15);

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
