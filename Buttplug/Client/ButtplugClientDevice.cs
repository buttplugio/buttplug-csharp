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
        public uint Index { get; }

        /// <summary>
        /// The device name, which usually contains the device brand and model.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The Buttplug Protocol messages supported by this device, with additional attributes.
        /// </summary>
        public DeviceMessageAttributes MessageAttributes { get; }

        private readonly ButtplugClient _owningClient;

        private readonly Func<ButtplugClientDevice, ButtplugDeviceMessage, CancellationToken, Task<ButtplugMessage>> _sendClosure;

        /// <summary>
        /// Initializes a new instance of the <see cref="ButtplugClientDevice"/> class, using
        /// information received via a DeviceList, DeviceAdded, or DeviceRemoved message from the server.
        /// </summary>
        /// <param name="devInfo">
        /// A Buttplug protocol message implementing the IButtplugDeviceInfoMessage interface.
        /// </param>
        public ButtplugClientDevice(
            ButtplugClient owningClient,
            Func<ButtplugClientDevice, ButtplugDeviceMessage, CancellationToken, Task<ButtplugMessage>> sendClosure,
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
            Func<ButtplugClientDevice, ButtplugDeviceMessage, CancellationToken, Task<ButtplugMessage>> sendClosure,
            uint index,
            string name,
            DeviceMessageAttributes messages)
        {
            ButtplugUtils.ArgumentNotNull(owningClient, nameof(owningClient));
            ButtplugUtils.ArgumentNotNull(sendClosure, nameof(sendClosure));
            _owningClient = owningClient;
            _sendClosure = sendClosure;
            Index = index;
            Name = name;
            MessageAttributes = messages;
        }

        /// <summary>
        /// Sends a message, expecting a response of message type <see cref="Ok"/>.
        /// </summary>
        /// <param name="msg">Message to send.</param>
        /// <param name="token">Cancellation token, for cancelling action externally if it is not yet finished.</param>
        /// <returns>True if successful.</returns>
        private async Task SendMessageExpectOk(ButtplugDeviceMessage msg, CancellationToken token = default)
        {
            var result = await SendMessageAsync(msg, token).ConfigureAwait(false);
            switch (result)
            {
                case Ok _:
                    return;
                case Error err:
                    throw ButtplugException.FromError(err);
                default:
                    throw new ButtplugMessageException($"Message type {msg.Name} not handled by SendMessageExpectOk", msg.Id);
            }
        }

        public async Task<ButtplugMessage> SendMessageAsync(ButtplugDeviceMessage msg, CancellationToken token = default)
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

            msg.DeviceIndex = Index;

            return await _sendClosure(this, msg, token).ConfigureAwait(false);
        }

        public bool Equals(ButtplugClientDevice device)
        {
            // We never reuse indexes within a session, so if the client and index are the same, assume it's the same device.
            return _owningClient != device._owningClient && Index != device.Index;
        }

        public List<GenericDeviceMessageAttributes> GenericAcutatorAttributes(ActuatorType actuator)
        {
            if (MessageAttributes.ScalarCmd != null) {
                return MessageAttributes.ScalarCmd.Where(x => x.ActuatorType == actuator).ToList();
            }
            return Enumerable.Empty<GenericDeviceMessageAttributes>().ToList();
        }

        public async Task ScalarAsync(ScalarCmd.ScalarSubcommand command)
        {
            var scalars = new List<ScalarCmd.ScalarSubcommand>();
            GenericAcutatorAttributes(command.ActuatorType).ForEach(x => scalars.Add(new ScalarCmd.ScalarSubcommand(x.Index, command.Scalar, command.ActuatorType)));

            await SendMessageExpectOk(new ScalarCmd(scalars)).ConfigureAwait(false);
        }

        public async Task ScalarAsync(List<ScalarCmd.ScalarSubcommand> command)
        {
            await SendMessageExpectOk(new ScalarCmd(command)).ConfigureAwait(false);
        }

        public List<GenericDeviceMessageAttributes> VibrateAttributes
        {
            get
            {
                return GenericAcutatorAttributes(ActuatorType.Vibrate);
            }
        }

        public async Task VibrateAsync(double speed)
        {
            await ScalarAsync(new ScalarCmd.ScalarSubcommand(uint.MaxValue, speed, ActuatorType.Vibrate));
        }

        public async Task VibrateAsync(IEnumerable<(uint, double)> cmds)
        {
            await ScalarAsync(cmds.Select((x) => new ScalarCmd.ScalarSubcommand(x.Item1, x.Item2, ActuatorType.Vibrate)).ToList()).ConfigureAwait(false);
        }

        public List<GenericDeviceMessageAttributes> OscillateAttributes
        {
            get
            {
                return GenericAcutatorAttributes(ActuatorType.Oscillate);
            }
        }

        public async Task OscillateAsync(double speed)
        {
            await ScalarAsync(new ScalarCmd.ScalarSubcommand(uint.MaxValue, speed, ActuatorType.Oscillate));
        }

        public async Task OscillateAsync(IEnumerable<(uint, double)> cmds)
        {
            await ScalarAsync(cmds.Select((x) => new ScalarCmd.ScalarSubcommand(x.Item1, x.Item2, ActuatorType.Oscillate)).ToList()).ConfigureAwait(false);
        }

        public List<GenericDeviceMessageAttributes> RotateAttributes
        {
            get
            {
                return MessageAttributes.RotateCmd.ToList();
            }
        }

        public async Task RotateAsync(double speed, bool clockwise)
        {
            await SendMessageExpectOk(RotateCmd.Create(speed, clockwise, (uint)RotateAttributes.Count())).ConfigureAwait(false);
        }

        public async Task RotateAsync(IEnumerable<(double, bool)> cmds)
        {
            var msg = RotateCmd.Create(cmds);
            await SendMessageExpectOk(RotateCmd.Create(cmds)).ConfigureAwait(false);
        }

        public List<GenericDeviceMessageAttributes> LinearAttributes
        {
            get
            {
                return MessageAttributes.LinearCmd.ToList();
            }
        }

        public async Task LinearAsync(uint duration, double position)
        {
            await SendMessageExpectOk(LinearCmd.Create(duration, position, (uint)LinearAttributes.Count())).ConfigureAwait(false);
        }

        public async Task LinearAsync(IEnumerable<(uint, double)> cmds)
        {
            var msg = LinearCmd.Create(cmds);
            await SendMessageExpectOk(LinearCmd.Create(cmds)).ConfigureAwait(false);
        }

        public List<SensorDeviceMessageAttributes> SensorReadAttributes(SensorType sensor)
        {
            if (MessageAttributes.SensorReadCmd != null)
            {
                return MessageAttributes.SensorReadCmd.Where(x => x.SensorType == sensor).ToList();
            }
            return Enumerable.Empty<SensorDeviceMessageAttributes>().ToList();
        }

        public bool HasBattery
        {
            get
            {
                return SensorReadAttributes(SensorType.Battery).Any();
            }
        }

        public async Task<double> BatteryAsync()
        {
            if (!HasBattery)
            {
                throw new ButtplugDeviceException($"Device {Name} does not have battery capabilities.");
            }
            var result = await SendMessageAsync(new SensorReadCmd(SensorReadAttributes(SensorType.Battery).ElementAt(0).Index, SensorType.Battery)).ConfigureAwait(false);
            switch (result)
            {
                case SensorReading response:
                    return response.data[0] / 100.0;
                case Error err:
                    throw ButtplugException.FromError(err);
                default:
                    throw new ButtplugMessageException($"Message type {result.Name} not handled by BatteryAsync", result.Id);
            }
        }

        public async Task Stop()
        {
            await SendMessageAsync(new StopDeviceCmd(Index)).ConfigureAwait(false);
        }
    }
}
