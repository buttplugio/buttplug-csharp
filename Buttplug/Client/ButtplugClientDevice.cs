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
        /// <param name="aDevInfo">
        /// A Buttplug protocol message implementing the IButtplugDeviceInfoMessage interface.
        /// </param>
        public ButtplugClientDevice(
            ButtplugClient aOwningClient,
            Func<ButtplugClientDevice, ButtplugDeviceMessage, CancellationToken, Task> aSendClosure,
            IButtplugDeviceInfoMessage aDevInfo)
           : this(aOwningClient, aSendClosure, aDevInfo.DeviceIndex, aDevInfo.DeviceName, aDevInfo.DeviceMessages)
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
        public ButtplugClientDevice(
            ButtplugClient aOwningClient,
            Func<ButtplugClientDevice, ButtplugDeviceMessage, CancellationToken, Task> aSendClosure,
            uint aIndex,
            string aName,
            Dictionary<string, MessageAttributes> aMessages)
        {
            ButtplugUtils.ArgumentNotNull(aOwningClient, nameof(aOwningClient));
            ButtplugUtils.ArgumentNotNull(aSendClosure, nameof(aSendClosure));
            this._owningClient = aOwningClient;
            this._sendClosure = aSendClosure;
            this.Index = aIndex;
            this.Name = aName;
            this.AllowedMessages = new Dictionary<Type, MessageAttributes>();
            foreach (var msg in aMessages)
            {
                var msgType = ButtplugUtils.GetMessageType(msg.Key);
                if (msgType == null)
                {
                    throw new ButtplugDeviceException($"Message type {msg.Key} does not exist.");
                }

                this.AllowedMessages[msgType] = msg.Value;
            }
        }

        public MessageAttributes GetMessageAttributes(Type aMsgType)
        {
            ButtplugUtils.ArgumentNotNull(aMsgType, nameof(aMsgType));
            if (!aMsgType.IsSubclassOf(typeof(ButtplugDeviceMessage)))
            {
                throw new ArgumentException("Argument must be subclass of ButtplugDeviceMessage");
            }

            if (!this.AllowedMessages.ContainsKey(aMsgType))
            {
                throw new ButtplugDeviceException($"Message type {aMsgType.Name} not allowed for device {this.Name}.");
            }

            return this.AllowedMessages[aMsgType];
        }

        public MessageAttributes GetMessageAttributes<T>()
        where T : ButtplugDeviceMessage
        {
            return this.GetMessageAttributes(typeof(T));
        }

        public async Task SendMessageAsync(ButtplugDeviceMessage aMsg, CancellationToken aToken = default(CancellationToken))
        {
            ButtplugUtils.ArgumentNotNull(aMsg, nameof(aMsg));

            if (!this._owningClient.Connected)
            {
                throw new ButtplugClientConnectorException("Client that owns device is not connected");
            }

            if (!this._owningClient.Devices.Contains(this))
            {
                throw new ButtplugDeviceException("Device no longer connected or valid");
            }

            if (!this.AllowedMessages.ContainsKey(aMsg.GetType()))
            {
                throw new ButtplugDeviceException(
                    $"Device {this.Name} does not support message type {aMsg.GetType().Name}");
            }

            aMsg.DeviceIndex = this.Index;

            await this._sendClosure(this, aMsg, aToken).ConfigureAwait(false);
        }

        public bool Equals(ButtplugClientDevice aDevice)
        {
            if (this._owningClient != aDevice._owningClient ||
                this.Index != aDevice.Index ||
                this.Name != aDevice.Name ||
                this.AllowedMessages.Count != aDevice.AllowedMessages.Count ||
                !this.AllowedMessages.Keys.SequenceEqual(aDevice.AllowedMessages.Keys))
            {
                return false;
            }

            // If we made it this far, do actual value comparison in the attributes
            try
            {
                // https://stackoverflow.com/a/9547463/4040754
                return !aDevice.AllowedMessages.Where(entry => !this.AllowedMessages[entry.Key].Equals(entry.Value))
                    .ToDictionary(entry => entry.Key, entry => entry.Value).Any();
            }
            catch
            {
                return false;
            }
        }

        public void CheckGenericSubcommandList<T>(IEnumerable<T> aCmdList, uint aLimitValue)
            where T : GenericMessageSubcommand
        {
            if (!aCmdList.Any() || aCmdList.Count() > aLimitValue)
            {
                if (aLimitValue == 1)
                {
                    throw new ButtplugDeviceException($"{typeof(T).Name} requires 1 subcommand for this device, {aCmdList.Count()} present.");
                }

                throw new ButtplugDeviceException($"{typeof(T).Name} requires between 1 and {aLimitValue} subcommands for this device, {aCmdList.Count()} present.");
            }

            foreach (var cmd in aCmdList)
            {
                if (cmd.Index >= aLimitValue)
                {
                    throw new ButtplugDeviceException($"Index {cmd.Index} is out of bounds for {typeof(T).Name} for this device.");
                }
            }
        }

        private void CheckAllowedMessageType<T>()
        where T : ButtplugDeviceMessage
        {
            if (!this.AllowedMessages.ContainsKey(typeof(T)))
            {
                throw new ButtplugDeviceException($"Device {this.Name} does not support message type {typeof(T).Name}");
            }
        }

        public async Task SendVibrateCmd(double aSpeed)
        {
            this.CheckAllowedMessageType<VibrateCmd>();
            await this.SendMessageAsync(VibrateCmd.Create(aSpeed, this.GetMessageAttributes<VibrateCmd>().FeatureCount.Value)).ConfigureAwait(false);
        }

        public async Task SendVibrateCmd(IEnumerable<double> aCmds)
        {
            this.CheckAllowedMessageType<VibrateCmd>();
            var msg = VibrateCmd.Create(aCmds);
            this.CheckGenericSubcommandList(msg.Speeds, this.GetMessageAttributes<VibrateCmd>().FeatureCount.Value);
            await this.SendMessageAsync(VibrateCmd.Create(aCmds)).ConfigureAwait(false);
        }

        public async Task SendRotateCmd(double aSpeed, bool aClockwise)
        {
            this.CheckAllowedMessageType<RotateCmd>();
            await this.SendMessageAsync(RotateCmd.Create(aSpeed, aClockwise, this.GetMessageAttributes<RotateCmd>().FeatureCount.Value)).ConfigureAwait(false);
        }

        public async Task SendRotateCmd(IEnumerable<(double, bool)> aCmds)
        {
            this.CheckAllowedMessageType<RotateCmd>();
            var msg = RotateCmd.Create(aCmds);
            this.CheckGenericSubcommandList(msg.Rotations, this.GetMessageAttributes<RotateCmd>().FeatureCount.Value);
            await this.SendMessageAsync(RotateCmd.Create(aCmds)).ConfigureAwait(false);
        }

        public async Task SendLinearCmd(uint aDuration, double aPosition)
        {
            this.CheckAllowedMessageType<LinearCmd>();
            await this.SendMessageAsync(LinearCmd.Create(aDuration, aPosition, this.GetMessageAttributes<LinearCmd>().FeatureCount.Value)).ConfigureAwait(false);
        }

        public async Task SendLinearCmd(IEnumerable<(uint, double)> aCmds)
        {
            this.CheckAllowedMessageType<LinearCmd>();
            var msg = LinearCmd.Create(aCmds);
            this.CheckGenericSubcommandList(msg.Vectors, this.GetMessageAttributes<LinearCmd>().FeatureCount.Value);
            await this.SendMessageAsync(LinearCmd.Create(aCmds)).ConfigureAwait(false);
        }

        public async Task SendFleshlightLaunchFW12Cmd(uint aSpeed, uint aPosition)
        {
            this.CheckAllowedMessageType<FleshlightLaunchFW12Cmd>();
            await this.SendMessageAsync(new FleshlightLaunchFW12Cmd(this.Index, aSpeed, aPosition)).ConfigureAwait(false);
        }

        public async Task SendLovenseCmd(string aDeviceCmd)
        {
            this.CheckAllowedMessageType<LovenseCmd>();
            await this.SendMessageAsync(new LovenseCmd(this.Index, aDeviceCmd)).ConfigureAwait(false);
        }

        public async Task SendVorzeA10CycloneCmd(uint aSpeed, bool aClockwise)
        {
            this.CheckAllowedMessageType<VorzeA10CycloneCmd>();
            await this.SendMessageAsync(new VorzeA10CycloneCmd(this.Index, aSpeed, aClockwise)).ConfigureAwait(false);
        }

        public async Task StopDeviceCmd()
        {
            // Every message should support this, but it doesn't hurt to check
            this.CheckAllowedMessageType<StopDeviceCmd>();
            await this.SendMessageAsync(new StopDeviceCmd(this.Index)).ConfigureAwait(false);
        }

        public async Task KiirooCmd(uint aPosition)
        {
            this.CheckAllowedMessageType<KiirooCmd>();
            await this.SendMessageAsync(new KiirooCmd(this.Index, aPosition)).ConfigureAwait(false);
        }
    }
}
