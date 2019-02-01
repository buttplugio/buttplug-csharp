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
using Buttplug.Core.Messages;
using Buttplug.Devices;
using SharpDX.XInput;

namespace Buttplug.Server.Managers.XInputGamepadManager
{
    internal class XInputGamepadDevice : ButtplugDeviceImpl
    {
        private Controller _device;

        public XInputGamepadDevice(IButtplugLogManager aLogManager, Controller aDevice)
            : base(aLogManager)
        {
            _device = aDevice;
        }

        public override Task<ButtplugMessage> WriteValueAsync(uint aMsgId, byte[] aValue, bool aWriteWithResponse,
            CancellationToken aToken)
        {
            if (aValue.Length != 4)
            {
                throw new ButtplugDeviceException(BpLogger, "XInput requires 4 byte inputs.", aMsgId);
            }

            // This assumes we're getting the values in the correct endianness for the platform when
            // reconstructing the ushorts.
            var v = new Vibration
            {
                LeftMotorSpeed = BitConverter.ToUInt16(aValue, 0),
                RightMotorSpeed = BitConverter.ToUInt16(aValue, 2),
            };

            _device.SetVibration(v);

            // Nothing to await here.
            return Task.FromResult<ButtplugMessage>(new Ok(aMsgId));
        }

        public override bool Connected => _device != null;

        public override void Disconnect()
        {
            _device = null;
        }

        // Unused for Gamepad controllers currently.
        public override Task<ButtplugMessage> WriteValueAsync(uint aMsgId, string aEndpointName, byte[] aValue, bool aWriteWithResponse,
            CancellationToken aToken)
        {
            throw new NotImplementedException();
        }

        // Unused for Gamepad controllers currently.
        public override Task<(ButtplugMessage, byte[])> ReadValueAsync(uint aMsgId, CancellationToken aToken)
        {
            throw new NotImplementedException();
        }

        // Unused for Gamepad controllers currently.
        public override Task<(ButtplugMessage, byte[])> ReadValueAsync(uint aMsgId, string aEndpointName, CancellationToken aToken)
        {
            throw new NotImplementedException();
        }

        // Unused for Gamepad controllers currently.
        public override Task SubscribeToUpdatesAsync()
        {
            throw new NotImplementedException();
        }

        // Unused for Gamepad controllers currently.
        public override Task SubscribeToUpdatesAsync(string aEndpointName)
        {
            throw new NotImplementedException();
        }
    }
}
