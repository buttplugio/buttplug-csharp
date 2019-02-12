namespace Buttplug.Devices
{
    public class ButtplugDeviceReadOptions
    {
        public string Endpoint = Endpoints.Tx;
        public uint Timeout = int.MaxValue;
        public uint ReadLength = int.MaxValue;
    }
}
