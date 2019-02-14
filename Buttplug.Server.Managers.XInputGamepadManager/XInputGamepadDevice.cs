// <copyright file="XInputGamepadDevice.cs" company="Nonpolynomial Labs LLC">
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
using SharpDX.XInput;

namespace Buttplug.Server.Managers.XInputGamepadManager
{
    internal class XInputGamepadDevice : ButtplugDeviceImpl
    {
        private Controller _device;

        public override bool Connected => _device != null;

        public XInputGamepadDevice(IButtplugLogManager aLogManager, Controller aDevice)
            : base(aLogManager)
        {
            _device = aDevice;
            Address = $"XInput{aDevice.UserIndex.ToString()}";
        }

        public override Task WriteValueAsyncInternal(byte[] aValue,
            ButtplugDeviceWriteOptions aOptions,
            CancellationToken aToken = default(CancellationToken))
        {
            if (aValue.Length != 4)
            {
                throw new ButtplugDeviceException(BpLogger, "XInput requires 4 byte inputs.");
            }

            // This assumes we're getting the values in the correct endianness for the platform when
            // reconstructing the ushorts.
            var v = new Vibration
            {
                LeftMotorSpeed = BitConverter.ToUInt16(aValue, 0),
                RightMotorSpeed = BitConverter.ToUInt16(aValue, 2),
            };

            _device.SetVibration(v);

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

        public override void Disconnect()
        {
            _device = null;
        }
    }
}
