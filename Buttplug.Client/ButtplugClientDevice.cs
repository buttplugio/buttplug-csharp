// <copyright file="ButtplugClientDevice.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using Buttplug.Core;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="ButtplugClientDevice"/> class, using
        /// information received via a DeviceList, DeviceAdded, or DeviceRemoved message from the server.
        /// </summary>
        /// <param name="aDevInfo">
        /// A Buttplug protocol message implementing the IButtplugDeviceInfoMessage interface.
        /// </param>
        public ButtplugClientDevice(IButtplugDeviceInfoMessage aDevInfo)
           : this(aDevInfo.DeviceIndex, aDevInfo.DeviceName, aDevInfo.DeviceMessages)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ButtplugClientDevice"/> class, using
        /// discrete parameters.
        /// </summary>
        /// <param name="aIndex">The device index.</param>
        /// <param name="aName">The device name.</param>
        /// <param name="aMessages">The device allowed message list, with corresponding attributes.</param>
        public ButtplugClientDevice(uint aIndex, string aName, Dictionary<string, MessageAttributes> aMessages)
        {
            Index = aIndex;
            Name = aName;
            AllowedMessages = aMessages;
        }

        public bool Equals(ButtplugClientDevice aDevice)
        {
            if (Index != aDevice.Index ||
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
