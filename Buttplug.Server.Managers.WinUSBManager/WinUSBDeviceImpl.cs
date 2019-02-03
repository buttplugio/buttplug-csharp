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

        public override Task WriteValueAsync(byte[] aValue, CancellationToken aToken)
        {
            throw new NotImplementedException();
        }

        public override Task WriteValueAsync(string aEndpointName, byte[] aValue, CancellationToken aToken)
        {
            // todo Throw or bail if we have nothing to send.
            if (aEndpointName == Endpoints.TxVendorControl)
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
                throw new ButtplugDeviceException(BpLogger, $"Unknown endpoint {aEndpointName}");
            }

            return Task.CompletedTask;
        }

        public override Task WriteValueAsync(byte[] aValue, bool aWriteWithResponse, CancellationToken aToken)
        {
            throw new NotImplementedException();
        }

        public override Task WriteValueAsync(string aEndpointName, byte[] aValue, bool aWriteWithResponse, CancellationToken aToken)
        {
            throw new NotImplementedException();
        }

        public override Task<byte[]> ReadValueAsync(CancellationToken aToken)
        {
            throw new NotImplementedException();
        }

        public override Task<byte[]> ReadValueAsync(string aEndpointName, CancellationToken aToken)
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
