// <copyright file="HidSharpDeviceImpl.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Logging;
using Buttplug.Devices;
using HidSharp;

namespace Buttplug.Server.Managers.HidSharpManager
{
    public class HidSharpHidDeviceImpl : ButtplugDeviceImpl
    {
        private HidDevice _device;
        private HidStream _stream;

        public override bool Connected => _stream != null;

        public HidSharpHidDeviceImpl(IButtplugLogManager aLogManager, HidDevice aDevice)
            : base(aLogManager)
        {
            _device = aDevice;
            aDevice.TryOpen(out _stream);
            Name = _device.GetProductName();
            Address = aDevice.DevicePath;
        }

        public override void Disconnect()
        {
            _stream.Close();
            _stream = null;
            _device = null;
        }

        public override async Task WriteValueAsyncInternal(byte[] aValue,
            ButtplugDeviceWriteOptions aOptions,
            CancellationToken aToken = default(CancellationToken))
        {
            // Both Hid and Serial only have one outgoing endpoint.
            if (aOptions.Endpoint != Endpoints.Tx)
            {
                throw new ButtplugDeviceException(BpLogger, "HidDevice doesn't support any write endpoint except the default.");
            }
            await _stream.WriteAsync(aValue, 0, aValue.Length, aToken).ConfigureAwait(false);
        }

        public override async Task<byte[]> ReadValueAsyncInternal(ButtplugDeviceReadOptions aOptions,
            CancellationToken aToken = default(CancellationToken))
        {
            // Both Hid and Serial only have one incoming endpoint.
            if (aOptions.Endpoint != Endpoints.Rx)
            {
                throw new ButtplugDeviceException(BpLogger, "HidDevice doesn't support any read endpoint except the default.");
            }

            var arr = new byte[64];
            var read = await _stream.ReadAsync(arr, 0, arr.Length, aToken).ConfigureAwait(false);
            return arr;
        }

        public override Task SubscribeToUpdatesAsyncInternal(ButtplugDeviceReadOptions aOptions)
        {
            throw new NotImplementedException();
        }
    }
}
