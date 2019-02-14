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
            _device.WriteTimeout = 1000;
            Address = _device.Device.DevicePath;
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

        public override async Task WriteValueAsyncInternal(byte[] aValue, ButtplugDeviceWriteOptions aOptions,
            CancellationToken aToken = default(CancellationToken))
        {
            if (aOptions.Endpoint != Endpoints.Tx)
            {
                throw new ButtplugDeviceException(BpLogger, "Device only supports the tx endpoint.");
            }
            await _device.WriteAsync(aValue, 0, aValue.Length, aToken);
        }

        public override async Task<byte[]> ReadValueAsyncInternal(ButtplugDeviceReadOptions aOptions,
            CancellationToken aToken = default(CancellationToken))
        {
            if (!_device.CanRead)
            {
                return new byte[] { };
            }

            var oldTimeout = 0;
            if (aOptions.Timeout < int.MaxValue)
            {
                oldTimeout = _device.ReadTimeout;
                _device.ReadTimeout = (int)aOptions.Timeout;
            }

            //var bytesToRead = _device.Length;
            var input = new byte[aOptions.ReadLength];
            var readLength = 0;
            try
            {
                while (readLength < aOptions.ReadLength)
                {
                    readLength += await _device.ReadAsync(input, readLength, (int) aOptions.ReadLength - readLength);
                }
            }
            finally
            {
                if (aOptions.Timeout < int.MaxValue)
                {
                    _device.ReadTimeout = oldTimeout;
                }
            }

            return input;
        }

        public override Task SubscribeToUpdatesAsyncInternal(ButtplugDeviceReadOptions aOptions)
        {
            throw new NotImplementedException();
        }
    }
}
