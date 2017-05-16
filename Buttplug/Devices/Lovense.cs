using Buttplug.Core;
using Buttplug.Messages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Buttplug.Devices
{
    internal class LovenseBluetoothInfo : IBluetoothDeviceInfo
    {
        public Guid[] Services { get; } = { new Guid("6e400001-b5a3-f393-e0a9-e50e24dcca9e") };
        public string[] Names { get; } = { "LVS-S001", "LVS-Z001" };

        public Guid[] Characteristics { get; } =
        {
            // tx characteristic
            new Guid("6e400002-b5a3-f393-e0a9-e50e24dcca9e"),
            // rx characteristic
            new Guid("6e400003-b5a3-f393-e0a9-e50e24dcca9e")
        };

        public ButtplugBluetoothDevice CreateDevice(BluetoothLEDevice aDevice,
                                                    Dictionary<Guid, GattCharacteristic> aCharacteristics)
        {
            return new Lovense(aDevice,
                               aCharacteristics[Characteristics[0]],
                               aCharacteristics[Characteristics[1]]);
        }
    }

    internal class Lovense : ButtplugBluetoothDevice
    {


        public Lovense(BluetoothLEDevice aDevice,
                       GattCharacteristic aWriteChr,
                       GattCharacteristic aReadChr) :
            base($"Lovense Device ({aDevice.Name})", 
                 aDevice,
                 aWriteChr,
                 aReadChr)
        {
            MsgFuncs.Add(typeof(SingleMotorVibrateCmd), HandleSingleMotorVibrateCmd);
        }

        public async Task<ButtplugMessage> HandleSingleMotorVibrateCmd(ButtplugDeviceMessage aMsg)
        {
            var cmdMsg = aMsg as SingleMotorVibrateCmd;
            if (cmdMsg is null)
            {
                return ButtplugUtils.LogErrorMsg(aMsg.Id, BpLogger, "Wrong Handler");
            }
            var buf = ButtplugUtils.WriteString($"Vibrate:{(int)(cmdMsg.Speed * 20)};");
            return await WriteToDevice(aMsg, buf);
        }
    }
}