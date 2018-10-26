// <copyright file="ButtplugSerialDevice.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Devices;
using Buttplug.Core.Logging;

namespace Buttplug.Server.Managers.SerialPortManager
{
    public class ButtplugSerialDevice : ButtplugDevice
    {
        protected readonly SerialPort _port;

        public ButtplugSerialDevice(IButtplugLogManager aLogManager, string aName, string aIdentifier, SerialPort aPort)
            : base(aLogManager, aName, aIdentifier)
        {
            _port = aPort;
        }

        protected void Clear()
        {
            _port.DiscardInBuffer();
        }

        protected int ReadByte()
        {
            return _port.BaseStream.ReadByte();
        }

        protected async Task<byte[]> ReadAsync(int aLength, CancellationToken aToken = default(CancellationToken))
        {
            var c = 0;
            var retBuf = new byte[aLength];
            while (c < aLength)
            {
                c += await _port.BaseStream.ReadAsync(retBuf, c, aLength - c, aToken).ConfigureAwait(false);
            }

            return retBuf;
        }

        protected async Task WriteAsync(byte[] aBuffer, CancellationToken aToken = default(CancellationToken))
        {
            await WriteAsync(aBuffer, 0, aBuffer.Length, aToken).ConfigureAwait(false);
        }

        protected async Task WriteAsync(byte[] aBuffer, int aOffset, int aCount, CancellationToken aToken)
        {
            await _port.BaseStream.WriteAsync(aBuffer, aOffset, aCount, aToken).ConfigureAwait(false);
        }

        public override void Disconnect()
        {
            _port?.Close();
        }
    }
}