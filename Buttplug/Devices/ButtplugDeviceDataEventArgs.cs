namespace Buttplug.Devices
{
    public class ButtplugDeviceDataEventArgs
    {
        public byte[] bytes { get; }

        public ButtplugDeviceDataEventArgs(byte[] aBytes)
        {
            bytes = aBytes;
        }
    }
}