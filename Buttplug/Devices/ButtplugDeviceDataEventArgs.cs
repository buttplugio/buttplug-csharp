namespace Buttplug.Devices
{
    public class ButtplugDeviceDataEventArgs
    {
        public string Endpoint { get; }

        public byte[] Bytes { get; }

        public ButtplugDeviceDataEventArgs(string aEndpointName, byte[] aBytes)
        {
            Endpoint = aEndpointName;
            Bytes = aBytes;
        }
    }
}