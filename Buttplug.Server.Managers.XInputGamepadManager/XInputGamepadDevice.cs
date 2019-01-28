// <copyright file="XInputGamepadDevice.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
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

        public Task<ButtplugMessage> WriteValueAsync(uint aMsgId, ushort[] aValue, bool aWriteWithResponse,
            CancellationToken aToken)
        {
            var v = new Vibration
            {
                LeftMotorSpeed = aValue[0],
                RightMotorSpeed = aValue[1],
            };
            _device.SetVibration(v);
            // Nothing to await here.
            return Task.FromResult<ButtplugMessage>(new Ok(aMsgId));
        }

        public override void Disconnect()
        {
            _device = null;
        }
    }
}
