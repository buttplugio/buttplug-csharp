using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using Buttplug.Devices;

namespace Buttplug.Test
{
    public class TestDeviceImpl : ButtplugDeviceImpl
    {
        public override event EventHandler<ButtplugDeviceDataEventArgs> DataReceived;

        public override event EventHandler DeviceRemoved;

        public override string Name { get; }

        public override string Address { get; }

        private bool _connected;

        public override bool Connected => _connected;

        public TestDeviceImpl(IButtplugLogManager aLogManager)
            : base(aLogManager)
        {
            Name = "Test Device";
            Address = "Test";
            _connected = true;
        }

        public override void Disconnect()
        {
            _connected = false;
        }

        public override Task<ButtplugMessage> WriteValueAsync(uint aMsgId, byte[] aValue, bool aWriteWithResponse, CancellationToken aToken)
        {
            throw new NotImplementedException();
        }

        public override Task<ButtplugMessage> WriteValueAsync(uint aMsgId, string aEndpointName, byte[] aValue, bool aWriteWithResponse,
            CancellationToken aToken)
        {
            throw new NotImplementedException();
        }

        public override Task<(ButtplugMessage, byte[])> ReadValueAsync(uint aMsgId, CancellationToken aToken)
        {
            throw new NotImplementedException();
        }

        public override Task<(ButtplugMessage, byte[])> ReadValueAsync(uint aMsgId, string aEndpointName, CancellationToken aToken)
        {
            throw new NotImplementedException();
        }

        public override Task SubscribeToUpdatesAsync()
        {
            throw new NotImplementedException();
        }

        public override Task SubscribeToUpdatesAsync(string aEndpointName)
        {
            throw new NotImplementedException();
        }
    }
}
