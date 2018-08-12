﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace Buttplug.Core
{
    /// <summary>
    /// Interface for representations of hardware devices.
    /// </summary>
    public interface IButtplugDevice
    {
        /// <summary>
        /// Device name.
        /// </summary>
        [NotNull]
        string Name { get; }

        /// <summary>
        /// Device identifier. Something that uniquely identifies this device, such as a Bluetooth Address.
        /// </summary>
        [NotNull]
        string Identifier { get; }

        /// <summary>
        /// Index of the device.
        /// </summary>
        uint Index { get; set; }

        /// <summary>
        /// Value indicating whether the device is connected.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Event handler for device removal.
        /// </summary>
        [CanBeNull]
        event EventHandler DeviceRemoved;

        /// <summary>
        /// Event handler for device actions.
        /// </summary>
        [CanBeNull]
        event EventHandler<MessageReceivedEventArgs> MessageEmitted;

        /// <summary>
        /// Allowed message types for this device.
        /// </summary>
        /// <returns>Enumerable of message types</returns>
        [NotNull]
        IEnumerable<Type> GetAllowedMessageTypes();

        /// <summary>
        /// Checks to see whether a message is supported by the device that implements this
        /// interface. If the message is supported, executes the handler for that message.
        /// </summary>
        /// <param name="aMsg">Device message to handle</param>
        /// <returns>Response, usually <see cref="Ok"/> or <see cref="Error"/>, but can be other types.</returns>
        [NotNull]
        Task<ButtplugMessage> ParseMessage(ButtplugDeviceMessage aMsg, CancellationToken aToken = default(CancellationToken));

        /// <summary>
        /// Initializes a device. Required for devices that may require connection handshakes or
        /// similar on-connection setups.
        /// </summary>
        /// <returns>Response, usually <see cref="Ok"/> or <see cref="Error"/>.</returns>
        [NotNull]
        Task<ButtplugMessage> Initialize(CancellationToken aToken = default(CancellationToken));

        /// <summary>
        /// Disconnect device.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Retreives the message attributes for the device associated with this message. Used for
        /// retreiving information about feature counts in device command messages, etc...
        /// </summary>
        /// <param name="aMsg">Message type to fetch attributes for</param>
        [NotNull]
        MessageAttributes GetMessageAttrs(Type aMsg);
    }
}
