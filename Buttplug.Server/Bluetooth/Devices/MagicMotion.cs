using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;

namespace Buttplug.Server.Bluetooth.Devices
{
    internal class MagicMotionBluetoothInfo : IBluetoothDeviceInfo
    {
        public enum Chrs : uint
        {
            Tx = 0,
        }

        /*
         * This has 6 services! Not sure what does what yet
         *
         * 78667579-7b48-43db-b8c5-7928a6b0a335  // Magic Motion's primary
         * 00001800-0000-1000-8000-00805f9b34fb
         * 00001801-0000-1000-8000-00805f9b34fb
         * 3d3cbc0e-f76b-11e3-8fcd-b2227cce2b54  // Unknown service
         * 0000180f-0000-1000-8000-00805f9b34fb
         * 0000180a-0000-1000-8000-00805f9b34fb
         */

        public Guid[] Services { get; } = { new Guid("78667579-7b48-43db-b8c5-7928a6b0a335") };

        public string[] NamePrefixes { get; } = { };

        public string[] Names { get; } =
        {
            "Smart Mini Vibe",
        };

        public Dictionary<uint, Guid> Characteristics { get; } = new Dictionary<uint, Guid>()
        {
            // tx characteristic
            { (uint)Chrs.Tx, new Guid("78667579-a914-49a4-8333-aa3c0cd8fedc") },
        };

        public IButtplugDevice CreateDevice(IButtplugLogManager aLogManager,
            IBluetoothDeviceInterface aInterface)
        {
            return new MagicMotion(aLogManager, aInterface, this);
        }
    }

    internal class MagicMotion : ButtplugBluetoothDevice
    {
        private double _vibratorSpeed;

        public MagicMotion(IButtplugLogManager aLogManager,
                           IBluetoothDeviceInterface aInterface,
                           IBluetoothDeviceInfo aInfo)
            : base(aLogManager,
                   $"MagicMotion Device ({aInterface.Name})",
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
            if (!(aMsg is SingleMotorVibrateCmd cmdMsg))
            {
                return BpLogger.LogErrorMsg(aMsg.Id, Error.ErrorClass.ERROR_DEVICE, "Wrong Handler");
            }

            return await HandleVibrateCmd(VibrateCmd.Create(cmdMsg.DeviceIndex, cmdMsg.Id, cmdMsg.Speed, 1));
        }

        private async Task<ButtplugMessage> HandleVibrateCmd(ButtplugDeviceMessage aMsg)
        {
            if (!(aMsg is VibrateCmd cmdMsg))
            {
                return BpLogger.LogErrorMsg(aMsg.Id, Error.ErrorClass.ERROR_DEVICE, "Wrong Handler");
            }

            if (cmdMsg.Speeds.Count != 1)
            {
                return new Error(
                    "VibrateCmd requires 1 vector for this device.",
                    Error.ErrorClass.ERROR_DEVICE,
                    cmdMsg.Id);
            }

            foreach (var v in cmdMsg.Speeds)
            {
                if (v.Index != 0)
                {
                    return new Error(
                        $"Index {v.Index} is out of bounds for VibrateCmd for this device.",
                        Error.ErrorClass.ERROR_DEVICE,
                        cmdMsg.Id);
                }

                if (Math.Abs(v.Speed - _vibratorSpeed) < 0.001)
                {
                    return new Ok(cmdMsg.Id);
                }

                _vibratorSpeed = v.Speed;
            }

            var data = new byte[] { 0x0b, 0xff, 0x04, 0x0a, 0x32, 0x32, 0x00, 0x04, 0x08, 0x00, 0x64, 0x00 };
            data[9] = Convert.ToByte(_vibratorSpeed * byte.MaxValue);

            // While there are 3 lovense revs right now, all of the characteristic arrays are the same.
            return await Interface.WriteValue(aMsg.Id,
                (uint)MagicMotionBluetoothInfo.Chrs.Tx,
                data);
        }
    }
}
