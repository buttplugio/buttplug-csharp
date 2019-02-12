using System;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Logging;
using Buttplug.Devices;
using MadWizard.WinUSBNet;

namespace Buttplug.Server.Managers.WinUSBManager
{
    class WinUSBDeviceImpl : ButtplugDeviceImpl
    {
        private USBDevice _device;

        public WinUSBDeviceImpl(IButtplugLogManager aLogManager, USBDevice aDevice) : base(aLogManager)
        {
            _device = aDevice;
        }

        public override bool Connected => _device != null;

        public override void Disconnect()
        {
            // Just dispose of the object and call it good.
            _device = null;
        }

        public override Task WriteValueAsyncInternal(byte[] aValue,
            ButtplugDeviceWriteOptions aOptions = default(ButtplugDeviceWriteOptions),
            CancellationToken aToken = default(CancellationToken))
        {
            // todo Throw or bail if we have nothing to send.
            if (aOptions.Endpoint == Endpoints.TxVendorControl)
            {
                _device.ControlOut(
                    0x02 << 5 | // Vendor Type
                    0x01 | // Interface Recipient
                    0x00, // Out Enpoint
                    1,
                    aValue[0],
                    0);
            }
            else
            {
                throw new ButtplugDeviceException(BpLogger, $"Unknown endpoint {aOptions.Endpoint}");
            }

            return Task.CompletedTask;
        }

        public override Task<byte[]> ReadValueAsyncInternal(ButtplugDeviceReadOptions aOptions,
            CancellationToken aToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public override Task SubscribeToUpdatesAsyncInternal(ButtplugDeviceReadOptions aOptions)
        {
            throw new NotImplementedException();
        }
    }
}
