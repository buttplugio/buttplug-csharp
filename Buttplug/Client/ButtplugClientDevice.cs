// <copyright file="ButtplugClientDevice.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;

using Buttplug.Core.Messages;

namespace Buttplug.Client
{
    /// <summary>
    /// The Buttplug Client representation of a Buttplug Device connected to a server.
    /// </summary>
    public class ButtplugClientDevice
    {
        /// <summary>
        /// The device index, which uniquely identifies the device on the server.
        /// </summary>
        /// <remarks>
        /// If a device is removed, this may be the only populated field. If the same device
        /// reconnects, the index should be reused.
        /// </remarks>
        public uint Index => _deviceInfo.DeviceIndex;

        /// <summary>
        /// The device name, which usually contains the device brand and model.
        /// </summary>
        public string Name => _deviceInfo.DeviceName;

        public string DisplayName => _deviceInfo.DeviceDisplayName;

        public uint MessageTimingGap => _deviceInfo.DeviceMessageTimingGap;

        private readonly ButtplugClientMessageHandler _handler;
        private readonly DeviceMessageInfo _deviceInfo;
        private readonly Dictionary<uint, ButtplugClientDeviceFeature> _features = new Dictionary<uint, ButtplugClientDeviceFeature>();
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ButtplugClientDevice"/> class, using
        /// information received via a DeviceList, DeviceAdded, or DeviceRemoved message from the server.
        /// </summary>
        /// <param name="devInfo">
        /// A Buttplug protocol message implementing the IButtplugDeviceInfoMessage interface.
        /// </param>
        internal ButtplugClientDevice(
            ButtplugClientMessageHandler handler,
            DeviceMessageInfo devInfo)
        {
            _handler = handler;
            _deviceInfo = devInfo;
            foreach (var feature in devInfo.DeviceFeatures)
            {
                _features.Add(feature.Key, new ButtplugClientDeviceFeature(_deviceInfo.DeviceIndex, feature.Value, _handler));
            }
        }

        public async Task VibrateAsync(uint speed)
        {
            foreach (var feature in _features.Values)
            {
                if (feature.CanVibrate())
                {
                    await feature.VibrateAsync(speed);
                }
            }
        }
        
        public async Task Stop()
        {
            await _handler.SendMessageExpectOk(new StopDeviceCmd(Index)).ConfigureAwait(false);
        }
    }
}
