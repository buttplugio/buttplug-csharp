using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using LanguageExt;

namespace Buttplug
{
    abstract class ButtplugBluetoothDevice : ButtplugDevice, IEquatable<ButtplugBluetoothDevice>
    {
        protected BluetoothLEDevice BLEDevice;

        protected ButtplugBluetoothDevice(String aName, BluetoothLEDevice aDevice) :
            base(aName)
        {
            BLEDevice = aDevice;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as ButtplugBluetoothDevice);
        }

        public bool Equals(ButtplugBluetoothDevice other)
        {
            if (Object.ReferenceEquals(other, null))
            {
                return false;
            }

            if (Object.ReferenceEquals(this, other))
            {
                return true;
            }
            return BLEDevice.BluetoothAddress == other.GetAddress();
        }

        public ulong GetAddress()
        {
            return BLEDevice.BluetoothAddress;
        }
    }
}
