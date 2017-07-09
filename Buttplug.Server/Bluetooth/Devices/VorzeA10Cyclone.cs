using System;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Messages;
using static Buttplug.Messages.Error;

namespace Buttplug.Bluetooth.Devices
{
    internal class VorzeA10CycloneInfo : IBluetoothDeviceInfo
    {
        public enum Chrs : uint
        {
            Tx = 0,
        }

        public Guid[] Services { get; } = { new Guid("40ee1111-63ec-4b7f-8ce7-712efd55b90e") };

        public string[] Names { get; } = { "CycSA" };

        public Guid[] Characteristics { get; } =
        {
                // tx characteristic
                new Guid("40ee2222-63ec-4b7f-8ce7-712efd55b90e"),
        };

        public IButtplugDevice CreateDevice(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
        {
            return new VorzeA10Cyclone(aLogManager,
                aInterface);
        }
    }

    internal class VorzeA10Cyclone : ButtplugBluetoothDevice
    {
        public VorzeA10Cyclone(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
            : base(aLogManager,
                   "Vorze A10 Cyclone",
                   aInterface)
        {
            MsgFuncs.Add(typeof(VorzeA10CycloneCmd), HandleVorzeA10CycloneCmd);
            MsgFuncs.Add(typeof(StopDeviceCmd), HandleStopDeviceCmd);
        }

        private async Task<ButtplugMessage> HandleStopDeviceCmd(ButtplugDeviceMessage aMsg)
        {
            return await HandleVorzeA10CycloneCmd(new VorzeA10CycloneCmd(aMsg.DeviceIndex, 0, false, aMsg.Id));
        }

        private async Task<ButtplugMessage> HandleVorzeA10CycloneCmd(ButtplugDeviceMessage aMsg)
        {
            var cmdMsg = aMsg as VorzeA10CycloneCmd;
            if (cmdMsg is null)
            {
                return BpLogger.LogErrorMsg(aMsg.Id, ErrorClass.ERROR_DEVICE, "Wrong Handler");
            }

            var rawSpeed = (byte)(((byte)(cmdMsg.Clockwise ? 1 : 0)) << 7 | (byte)cmdMsg.Speed);
            return await Interface.WriteValue(aMsg.Id,
                (uint)VorzeA10CycloneInfo.Chrs.Tx,
                new byte[] { 0x01, 0x01, rawSpeed });
        }
    }
}