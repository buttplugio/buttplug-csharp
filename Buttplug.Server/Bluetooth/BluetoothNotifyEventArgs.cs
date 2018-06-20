using System;

namespace Buttplug.Server.Bluetooth
{
    public class BluetoothNotifyEventArgs : EventArgs
    {
        public byte[] bytes { get; }

        public BluetoothNotifyEventArgs(byte[] aBytes)
        {
            bytes = aBytes;
        }
    }
}
