using System;
using Windows.Devices.Bluetooth;

namespace Buttplug.Core
{
    internal abstract class ButtplugBluetoothDevice : ButtplugDevice, IEquatable<ButtplugBluetoothDevice>
    {
        protected BluetoothLEDevice BleDevice;

        protected ButtplugBluetoothDevice(string aName, BluetoothLEDevice aDevice) :
            base(aName)
        {
            BleDevice = aDevice;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ButtplugBluetoothDevice);
        }

        public bool Equals(ButtplugBluetoothDevice other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return BleDevice.BluetoothAddress == other.GetAddress();
        }

        public ulong GetAddress()
        {
            return BleDevice.BluetoothAddress;
        }
    }
}
