using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Logging;
using Buttplug.Devices;

namespace Buttplug.Server.Managers.XamarinBluetoothManager
{
    class XamarinBluetoothDeviceInterface : ButtplugDeviceImpl
    {
        public XamarinBluetoothDeviceInterface(IButtplugLogManager aLogManager) : base(aLogManager)
        {
        }

        public override bool Connected { get; }

        public override Task WriteValueAsyncInternal(byte[] aValue, ButtplugDeviceWriteOptions aOptions,
            CancellationToken aToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public override Task<byte[]> ReadValueAsyncInternal(ButtplugDeviceReadOptions aOptions, CancellationToken aToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public override Task SubscribeToUpdatesAsyncInternal(ButtplugDeviceReadOptions aOptions)
        {
            throw new NotImplementedException();
        }

        public override void Disconnect()
        {
            throw new NotImplementedException();
        }
    }
}
