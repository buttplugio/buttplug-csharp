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
        private FixedSizedQueue<LovenseDongleOutgoingMessage> _send;

        public LovenseDongleDeviceImpl(IButtplugLogManager aLogManager, string device_id, FixedSizedQueue<LovenseDongleOutgoingMessage> send_queue)
            : base(aLogManager)
        {
            Address = device_id;
            DeviceID = device_id;
            _send = send_queue;
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

        public override Task WriteValueAsyncInternal(byte[] aValue, ButtplugDeviceWriteOptions aOptions, CancellationToken aToken = default(CancellationToken))
        {
            var input = Encoding.ASCII.GetString(aValue);
            var msg = new LovenseDongleOutgoingMessage()
            {
                Type = LovenseDongleMessageType.Toy,
                Func = LovenseDongleMessageFunc.Command,
                Id = DeviceID,
                Command = input,
            };
            _send.Enqueue(msg);
            return Task.CompletedTask;
        }

        public override Task<byte[]> ReadValueAsyncInternal(ButtplugDeviceReadOptions aOptions, CancellationToken aToken = default(CancellationToken))
        {
            throw new ButtplugDeviceException("Lovense Dongle Manager: Direct reading not implemented");
        }

        public override Task SubscribeToUpdatesAsyncInternal(ButtplugDeviceReadOptions aOptions)
        {
            return Task.CompletedTask;
        }
    }
}
