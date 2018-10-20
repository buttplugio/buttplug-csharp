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
using Buttplug.Core.Devices;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace Buttplug.Client
{
    /// <summary>
    /// The Buttplug Client representation of a Buttplug Device connected to a server.
    /// </summary>
    public class ButtplugClientDevice : IEquatable<ButtplugClientDevice>
    {
        /// <summary>
        /// The device index, which uniquely identifies the device on the server.
        /// </summary>
        /// <remarks>
        /// If a device is removed, this may be the only populated field. If the same device
        /// reconnects, the index should be reused.
        /// </remarks>
        public readonly uint Index;

        /// <summary>
        /// The device name, which usually contains the device brand and model.
        /// </summary>
        [NotNull]
        public readonly string Name;

        /// <summary>
        /// The Buttplug Protocol messages supported by this device, with additional attributes.
        /// </summary>
        [NotNull]
        public readonly Dictionary<string, MessageAttributes> AllowedMessages;

        private readonly ButtplugClient _owningClient;

        private readonly Func<ButtplugClientDevice, ButtplugDeviceMessage, CancellationToken, Task> _sendClosure;

        [NotNull]
        private readonly IButtplugLog _bpLogger;
        private readonly IButtplugLogManager _bpLogManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ButtplugClientDevice"/> class, using
        /// information received via a DeviceList, DeviceAdded, or DeviceRemoved message from the server.
        /// </summary>
        /// <param name="aDevInfo">
        /// A Buttplug protocol message implementing the IButtplugDeviceInfoMessage interface.
        /// </param>
        public ButtplugClientDevice(IButtplugLogManager aLogManager,
            ButtplugClient aOwningClient,
            Func<ButtplugClientDevice, ButtplugDeviceMessage, CancellationToken, Task> aSendClosure,
            IButtplugDeviceInfoMessage aDevInfo)
           : this(aLogManager, aOwningClient, aSendClosure, aDevInfo.DeviceIndex, aDevInfo.DeviceName, aDevInfo.DeviceMessages)
        {
            ButtplugUtils.ArgumentNotNull(aDevInfo, nameof(aDevInfo));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ButtplugClientDevice"/> class, using
        /// discrete parameters.
        /// </summary>
        /// <param name="aIndex">The device index.</param>
        /// <param name="aName">The device name.</param>
        /// <param name="aMessages">The device allowed message list, with corresponding attributes.</param>
        public ButtplugClientDevice(IButtplugLogManager aLogManager,
            ButtplugClient aOwningClient,
            Func<ButtplugClientDevice, ButtplugDeviceMessage, CancellationToken, Task> aSendClosure,
            uint aIndex,
            string aName,
            Dictionary<string, MessageAttributes> aMessages)
        {
            ButtplugUtils.ArgumentNotNull(aLogManager, nameof(aLogManager));
            ButtplugUtils.ArgumentNotNull(aOwningClient, nameof(aOwningClient));
            ButtplugUtils.ArgumentNotNull(aSendClosure, nameof(aSendClosure));
            _bpLogManager = aLogManager;
            _bpLogger = _bpLogManager.GetLogger(GetType());
            _owningClient = aOwningClient;
            _sendClosure = aSendClosure;
            Index = aIndex;
            Name = aName;
            AllowedMessages = aMessages;
        }

        public async Task SendMessageAsync(ButtplugDeviceMessage aMsg, CancellationToken aToken = default(CancellationToken))
        {
            if (!_owningClient.Connected)
            {
                throw new ButtplugClientConnectorException(_bpLogger, "Client that owns device is not connected");
            }

            if (!_owningClient.Devices.Contains(this))
            {
                throw new ButtplugDeviceException(_bpLogger, "Device no longer connected or valid");
            }

            await _sendClosure(this, aMsg, aToken);
        }

        public bool Equals(ButtplugClientDevice aDevice)
        {
            if (_owningClient != aDevice._owningClient ||
                Index != aDevice.Index ||
                Name != aDevice.Name ||
                AllowedMessages.Count != aDevice.AllowedMessages.Count ||
                !AllowedMessages.Keys.SequenceEqual(aDevice.AllowedMessages.Keys))
            {
                return false;
            }

            // If we made it this far, do actual value comparison in the attributes
            try
            {
                // https://stackoverflow.com/a/9547463/4040754
                return !aDevice.AllowedMessages.Where(entry => !AllowedMessages[entry.Key].Equals(entry.Value))
                    .ToDictionary(entry => entry.Key, entry => entry.Value).Any();
            }
            catch
            {
                return false;
            }
        }
    }
}
