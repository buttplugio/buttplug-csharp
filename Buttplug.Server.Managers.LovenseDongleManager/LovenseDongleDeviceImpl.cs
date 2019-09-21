using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Logging;
using Buttplug.Devices;

namespace Buttplug.Server.Managers.LovenseDongleManager
{
    public class LovenseDongleDeviceImpl : ButtplugDeviceImpl
    {
        public string DeviceID { get; private set; }
        private bool _connected = true;
        FixedSizedQueue<string> send;

        public LovenseDongleDeviceImpl(IButtplugLogManager aLogManager, string device_id, FixedSizedQueue<string> send_queue)
            : base(aLogManager)
        {
            Address = device_id;
            DeviceID = device_id;
            send = send_queue;
        }

        public override bool Connected => _connected;

        public override void Disconnect()
        {
            _connected = false;
            InvokeDeviceRemoved();
        }

        public void ProcessData(string data)
        {
            this.InvokeDataReceived(new ButtplugDeviceDataEventArgs("rx", Encoding.ASCII.GetBytes(data)));
        }

        public override async Task WriteValueAsyncInternal(byte[] aValue, ButtplugDeviceWriteOptions aOptions, CancellationToken aToken = default(CancellationToken))
        {
            var input = Encoding.ASCII.GetString(aValue);
            string format = "{\"type\":\"toy\",\"func\":\"command\",\"id\":\"" + DeviceID + "\",\"cmd\":\"" + input + "\"}\r";
            send.Enqueue(format);
        }

        public override async Task<byte[]> ReadValueAsyncInternal(ButtplugDeviceReadOptions aOptions, CancellationToken aToken = default(CancellationToken))
        {
            throw new ButtplugDeviceException("Lovense Dongle Manager: Direct reading not implemented");
        }

        public override Task SubscribeToUpdatesAsyncInternal(ButtplugDeviceReadOptions aOptions)
        {
            return Task.CompletedTask;
        }
    }
}
