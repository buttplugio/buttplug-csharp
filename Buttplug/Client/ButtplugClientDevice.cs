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
        public readonly string Name;

        /// <summary>
        /// The Buttplug Protocol messages supported by this device, with additional attributes.
        /// </summary>
        public Dictionary<Type, MessageAttributes> AllowedMessages { get; }

        private readonly ButtplugClient _owningClient;

        private readonly Func<ButtplugClientDevice, ButtplugDeviceMessage, CancellationToken, Task> _sendClosure;

        /// <summary>
        /// Initializes a new instance of the <see cref="ButtplugClientDevice"/> class, using
        /// information received via a DeviceList, DeviceAdded, or DeviceRemoved message from the server.
        /// </summary>
        /// <param name="devInfo">
        /// A Buttplug protocol message implementing the IButtplugDeviceInfoMessage interface.
        /// </param>
        public ButtplugClientDevice(
            ButtplugClient owningClient,
            Func<ButtplugClientDevice, ButtplugDeviceMessage, CancellationToken, Task> sendClosure,
            IButtplugDeviceInfoMessage devInfo)
           : this(owningClient, sendClosure, devInfo.DeviceIndex, devInfo.DeviceName, devInfo.DeviceMessages)
        {
            ButtplugUtils.ArgumentNotNull(devInfo, nameof(devInfo));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ButtplugClientDevice"/> class, using
        /// discrete parameters.
        /// </summary>
        /// <param name="index">The device index.</param>
        /// <param name="name">The device name.</param>
        /// <param name="messages">The device allowed message list, with corresponding attributes.</param>
        public ButtplugClientDevice(
            ButtplugClient owningClient,
            Func<ButtplugClientDevice, ButtplugDeviceMessage, CancellationToken, Task> sendClosure,
            uint index,
            string name,
            Dictionary<string, MessageAttributes> messages)
        {
            ButtplugUtils.ArgumentNotNull(owningClient, nameof(owningClient));
            ButtplugUtils.ArgumentNotNull(sendClosure, nameof(sendClosure));
            _owningClient = owningClient;
            _sendClosure = sendClosure;
            Index = index;
            Name = name;
            AllowedMessages = new Dictionary<Type, MessageAttributes>();
            foreach (var msg in messages)
            {
                var msgType = ButtplugUtils.GetMessageType(msg.Key);
                if (msgType == null)
                {
                    throw new ButtplugDeviceException($"Message type {msg.Key} does not exist.");
                }

                AllowedMessages[msgType] = msg.Value;
            }
        }

        public MessageAttributes GetMessageAttributes(Type msgType)
        {
            ButtplugUtils.ArgumentNotNull(msgType, nameof(msgType));
            if (!msgType.IsSubclassOf(typeof(ButtplugDeviceMessage)))
            {
                throw new ArgumentException("Argument must be subclass of ButtplugDeviceMessage");
            }

            if (!AllowedMessages.ContainsKey(msgType))
            {
                throw new ButtplugDeviceException($"Message type {msgType.Name} not allowed for device {Name}.");
            }

            return AllowedMessages[msgType];
        }

        public MessageAttributes GetMessageAttributes<T>()
        where T : ButtplugDeviceMessage
        {
            return GetMessageAttributes(typeof(T));
        }

        public async Task SendMessageAsync(ButtplugDeviceMessage msg, CancellationToken token = default(CancellationToken))
        {
            ButtplugUtils.ArgumentNotNull(msg, nameof(msg));

            if (!_owningClient.Connected)
            {
                throw new ButtplugClientConnectorException("Client that owns device is not connected");
            }

            if (!_owningClient.Devices.Contains(this))
            {
                throw new ButtplugDeviceException("Device no longer connected or valid");
            }

            if (!AllowedMessages.ContainsKey(msg.GetType()))
            {
                throw new ButtplugDeviceException(
                    $"Device {Name} does not support message type {msg.GetType().Name}");
            }

            msg.DeviceIndex = Index;

            await _sendClosure(this, msg, token).ConfigureAwait(false);
        }

        public bool Equals(ButtplugClientDevice device)
        {
            if (_owningClient != device._owningClient ||
                Index != device.Index ||
                Name != device.Name ||
                AllowedMessages.Count != device.AllowedMessages.Count ||
                !AllowedMessages.Keys.SequenceEqual(device.AllowedMessages.Keys))
            {
                return false;
            }

            // If we made it this far, do actual value comparison in the attributes
            try
            {
                // https://stackoverflow.com/a/9547463/4040754
                return !device.AllowedMessages.Where(entry => !AllowedMessages[entry.Key].Equals(entry.Value))
                    .ToDictionary(entry => entry.Key, entry => entry.Value).Any();
            }
            catch
            {
                return false;
            }
        }

        public void CheckGenericSubcommandList<T>(IEnumerable<T> cmdList, uint limitValue)
            where T : GenericMessageSubcommand
        {
            if (!cmdList.Any() || cmdList.Count() > limitValue)
            {
                if (limitValue == 1)
                {
                    throw new ButtplugDeviceException($"{typeof(T).Name} requires 1 subcommand for this device, {cmdList.Count()} present.");
                }

                throw new ButtplugDeviceException($"{typeof(T).Name} requires between 1 and {limitValue} subcommands for this device, {cmdList.Count()} present.");
            }

            foreach (var cmd in cmdList)
            {
                if (cmd.Index >= limitValue)
                {
                    throw new ButtplugDeviceException($"Index {cmd.Index} is out of bounds for {typeof(T).Name} for this device.");
                }
            }
        }

        private void CheckAllowedMessageType<T>()
        where T : ButtplugDeviceMessage
        {
            if (!AllowedMessages.ContainsKey(typeof(T)))
            {
                throw new ButtplugDeviceException($"Device {Name} does not support message type {typeof(T).Name}");
            }
        }

        public async Task SendVibrateCmd(double speed)
        {
            CheckAllowedMessageType<VibrateCmd>();
            await SendMessageAsync(VibrateCmd.Create(speed, GetMessageAttributes<VibrateCmd>().FeatureCount.Value)).ConfigureAwait(false);
        }

        public async Task SendVibrateCmd(IEnumerable<double> cmds)
        {
            CheckAllowedMessageType<VibrateCmd>();
            var msg = VibrateCmd.Create(cmds);
            CheckGenericSubcommandList(msg.Speeds, GetMessageAttributes<VibrateCmd>().FeatureCount.Value);
            await SendMessageAsync(VibrateCmd.Create(cmds)).ConfigureAwait(false);
        }

        public async Task SendRotateCmd(double speed, bool clockwise)
        {
            CheckAllowedMessageType<RotateCmd>();
            await SendMessageAsync(RotateCmd.Create(speed, clockwise, GetMessageAttributes<RotateCmd>().FeatureCount.Value)).ConfigureAwait(false);
        }

        public async Task SendRotateCmd(IEnumerable<(double, bool)> cmds)
        {
            CheckAllowedMessageType<RotateCmd>();
            var msg = RotateCmd.Create(cmds);
            CheckGenericSubcommandList(msg.Rotations, GetMessageAttributes<RotateCmd>().FeatureCount.Value);
            await SendMessageAsync(RotateCmd.Create(cmds)).ConfigureAwait(false);
        }

        public async Task SendLinearCmd(uint duration, double position)
        {
            CheckAllowedMessageType<LinearCmd>();
            await SendMessageAsync(LinearCmd.Create(duration, position, GetMessageAttributes<LinearCmd>().FeatureCount.Value)).ConfigureAwait(false);
        }

        public async Task SendLinearCmd(IEnumerable<(uint, double)> cmds)
        {
            CheckAllowedMessageType<LinearCmd>();
            var msg = LinearCmd.Create(cmds);
            CheckGenericSubcommandList(msg.Vectors, GetMessageAttributes<LinearCmd>().FeatureCount.Value);
            await SendMessageAsync(LinearCmd.Create(cmds)).ConfigureAwait(false);
        }

        public async Task StopDeviceCmd()
        {
            // Every message should support this, but it doesn't hurt to check
            CheckAllowedMessageType<StopDeviceCmd>();
            await SendMessageAsync(new StopDeviceCmd(Index)).ConfigureAwait(false);
        }
    }
}
