using System;

namespace Buttplug.Server.Bluetooth
{
    public class BluetoothNotifyEventArgs : EventArgs
    {
        public byte[] bytes { get; }

        public uint? charIdx { get; }

        public DateTime dateTime { get; }

        public BluetoothNotifyEventArgs(byte[] aBytes, uint? aCharIdx, DateTime aDateTime)
        {
            bytes = aBytes;
            charIdx = aCharIdx;
            dateTime = aDateTime;
        }
    }
}
