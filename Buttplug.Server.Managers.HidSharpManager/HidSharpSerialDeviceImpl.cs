using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Logging;
using Buttplug.Devices;
using HidSharp;

namespace Buttplug.Server.Managers.HidSharpManager
{
    class HidSharpSerialDeviceImpl : ButtplugDeviceImpl
    {
        private SerialStream _device;
        private bool _connected = true;

        public HidSharpSerialDeviceImpl(IButtplugLogManager aLogManager, SerialStream aDevice) 
            : base(aLogManager)
        {
            _device = aDevice;
            _device.Closed += OnClosed;
            _device.ReadTimeout = 1000;
        }

        private void OnClosed(object obj, EventArgs aArgs)
        {
            _connected = false;
        }

        public override bool Connected => _connected;

        public override void Disconnect()
        {
            _device.Close();
        }

        public override async Task WriteValueAsync(byte[] aValue, CancellationToken aToken)
        {
            await _device.WriteAsync(aValue, 0, aValue.Length, aToken);
        }

        public override async Task WriteValueAsync(string aEndpointName, byte[] aValue, CancellationToken aToken)
        {
            if (aEndpointName != Endpoints.Tx)
            {
                throw new ButtplugDeviceException(BpLogger, "Device only supports the tx endpoint.");
            }
            await WriteValueAsync(aValue, aToken);
        }

        public override async Task WriteValueAsync(byte[] aValue, bool aWriteWithResponse, CancellationToken aToken)
        {
            await WriteValueAsync(aValue, aToken);
        }

        public override async Task WriteValueAsync(string aEndpointName, byte[] aValue, bool aWriteWithResponse, CancellationToken aToken)
        {
            await WriteValueAsync(aEndpointName, aValue, aToken);
        }

        public override async Task<byte[]> ReadValueAsync(CancellationToken aToken)
        {
            if (!_device.CanRead)
            {
                return new byte[] { };
            }

            //var bytesToRead = _device.Length;
            var input = new byte[256];
            var bytesRead = await _device.ReadAsync(input, 0, 256);
            var bytesOut = new byte[bytesRead];
            Array.Copy(input, bytesOut, bytesRead);
            return input;
        }

        public override Task<byte[]> ReadValueAsync(string aEndpointName, CancellationToken aToken)
        {
            if (aEndpointName != Endpoints.Rx)
            {
                throw new ButtplugDeviceException(BpLogger, "Device only supports the rx endpoint.");
            }

            return ReadValueAsync(aToken);
        }

        public override async Task<byte[]> ReadValueAsync(uint aLength, CancellationToken aToken)
        {
            if (!_device.CanRead)
            {
                return new byte[] { };
            }

            //var bytesToRead = _device.Length;
            var input = new byte[aLength];
            var readLength = 0;
            while (readLength < aLength)
            {
                readLength += await _device.ReadAsync(input, readLength, (int)aLength - readLength);
            }
            return input;
        }

        public override Task<byte[]> ReadValueAsync(string aEndpointName, uint aLength, CancellationToken aToken)
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
