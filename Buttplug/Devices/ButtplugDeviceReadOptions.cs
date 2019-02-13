namespace Buttplug.Devices
{
    public class ButtplugDeviceReadOptions
    {
        public string Endpoint = Endpoints.Rx;
        public uint Timeout = int.MaxValue;
        public uint ReadLength = int.MaxValue;
    }
}
