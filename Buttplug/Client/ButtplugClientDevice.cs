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
        public uint Index { get; }

        /// <summary>
        /// The device name, which usually contains the device brand and model.
        /// </summary>
        public string Name { get; }

        public string DisplayName { get; }

        public uint MessageTimingGap { get; }

        /// <summary>
        /// The Buttplug Protocol messages supported by this device, with additional attributes.
        /// </summary>
        public DeviceMessageAttributes MessageAttributes { get; }

        private readonly ButtplugClientMessageHandler _handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="ButtplugClientDevice"/> class, using
        /// information received via a DeviceList, DeviceAdded, or DeviceRemoved message from the server.
        /// </summary>
        /// <param name="devInfo">
        /// A Buttplug protocol message implementing the IButtplugDeviceInfoMessage interface.
        /// </param>
        internal ButtplugClientDevice(
            ButtplugClientMessageHandler handler,
            IButtplugDeviceInfoMessage devInfo)
           : this(handler, devInfo.DeviceIndex, devInfo.DeviceName, devInfo.DeviceMessages, devInfo.DeviceDisplayName, devInfo.DeviceMessageTimingGap)
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
        internal ButtplugClientDevice(
            ButtplugClientMessageHandler handler,
            uint index,
            string name,
            DeviceMessageAttributes messages,
            string displayName,
            uint messageTimingGap)
        {
            ButtplugUtils.ArgumentNotNull(handler, nameof(handler));
            _handler = handler;
            Index = index;
            Name = name;
            MessageAttributes = messages;
            DisplayName = displayName;
            MessageTimingGap = messageTimingGap;
        }

        public List<GenericDeviceMessageAttributes> GenericAcutatorAttributes(ActuatorType actuator)
        {
            if (MessageAttributes.ScalarCmd != null)
            {
                return MessageAttributes.ScalarCmd.Where(x => x.ActuatorType == actuator).ToList();
            }

            return Enumerable.Empty<GenericDeviceMessageAttributes>().ToList();
        }

        public async Task ScalarAsync(ScalarCmd.ScalarSubcommand command)
        {
            var scalars = new List<ScalarCmd.ScalarSubcommand>();
            GenericAcutatorAttributes(command.ActuatorType).ForEach(x => scalars.Add(new ScalarCmd.ScalarSubcommand(x.Index, command.Scalar, command.ActuatorType)));
            if (!scalars.Any())
            {
                throw new ButtplugDeviceException($"Scalar command for device {Name} did not generate any commands. Are you sure the device supports the ActuatorType sent?");
            }
            await _handler.SendMessageExpectOk(new ScalarCmd(Index, scalars)).ConfigureAwait(false);
        }

        public async Task ScalarAsync(List<ScalarCmd.ScalarSubcommand> command)
        {
            if (!command.Any())
            {
                throw new ArgumentException($"Command List for ScalarAsync must have at least 1 command.");
            }
            await _handler.SendMessageExpectOk(new ScalarCmd(Index, command)).ConfigureAwait(false);
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

        public async Task VibrateAsync(IEnumerable<double> cmds)
        {
            var attrs = VibrateAttributes;
            if (cmds.Count() > attrs.Count())
            {
                throw new ButtplugDeviceException($"Device {Name} only has {attrs.Count()} vibrators, but {cmds.Count()} commands given.");
            }
            await ScalarAsync(attrs.Select((x, i) => new ScalarCmd.ScalarSubcommand(x.Index, cmds.ElementAt(i), ActuatorType.Vibrate)).ToList()).ConfigureAwait(false);
        }

        public async Task VibrateAsync(IEnumerable<ScalarCmd.ScalarCommand> cmds)
        {
            await ScalarAsync(cmds.Select((x) => new ScalarCmd.ScalarSubcommand(x.index, x.scalar, ActuatorType.Vibrate)).ToList()).ConfigureAwait(false);
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

        public async Task OscillateAsync(IEnumerable<double> cmds)
        {
            var attrs = OscillateAttributes;
            if (cmds.Count() > attrs.Count())
            {
                throw new ButtplugDeviceException($"Device {Name} only has {attrs.Count()} vibrators, but {cmds.Count()} commands given.");
            }
            await ScalarAsync(attrs.Select((x, i) => new ScalarCmd.ScalarSubcommand(x.Index, cmds.ElementAt(i), ActuatorType.Oscillate)).ToList()).ConfigureAwait(false);
        }

        public async Task OscillateAsync(IEnumerable<ScalarCmd.ScalarCommand> cmds)
        {
            await ScalarAsync(cmds.Select((x) => new ScalarCmd.ScalarSubcommand(x.index, x.scalar, ActuatorType.Oscillate)).ToList()).ConfigureAwait(false);
        }

        public List<GenericDeviceMessageAttributes> RotateAttributes
        {
            get
            {
                if (MessageAttributes.RotateCmd != null)
                {
                    return MessageAttributes.RotateCmd.ToList();
                }
                return Enumerable.Empty<GenericDeviceMessageAttributes>().ToList();
            }
        }

        public async Task RotateAsync(double speed, bool clockwise)
        {
            if (!RotateAttributes.Any())
            {
                throw new ButtplugDeviceException($"Device {Name} does not support rotation");
            }
            var msg = RotateCmd.Create(speed, clockwise, (uint)RotateAttributes.Count);
            msg.DeviceIndex = Index;
            await _handler.SendMessageExpectOk(msg).ConfigureAwait(false);
        }

        public async Task RotateAsync(IEnumerable<RotateCmd.RotateCommand> cmds)
        {
            if (!RotateAttributes.Any()) 
            {
                throw new ButtplugDeviceException($"Device {Name} does not support rotation");
            }
            var msg = RotateCmd.Create(cmds);
            msg.DeviceIndex = Index;
            await _handler.SendMessageExpectOk(msg).ConfigureAwait(false);
        }

        public List<GenericDeviceMessageAttributes> LinearAttributes
        {
            get
            {
                if (MessageAttributes.LinearCmd != null)
                {
                    return MessageAttributes.LinearCmd.ToList();
                }
                return Enumerable.Empty<GenericDeviceMessageAttributes>().ToList();
            }
        }

        public async Task LinearAsync(uint duration, double position)
        {
            if (!LinearAttributes.Any())
            {
                throw new ButtplugDeviceException($"Device {Name} does not support linear position");
            }
            var msg = LinearCmd.Create(duration, position, (uint)LinearAttributes.Count);
            msg.DeviceIndex = Index;
            await _handler.SendMessageExpectOk(msg).ConfigureAwait(false);
        }

        public async Task LinearAsync(IEnumerable<LinearCmd.VectorCommand> cmds)
        {
            if (!LinearAttributes.Any())
            {
                throw new ButtplugDeviceException($"Device {Name} does not support linear position");
            }
            var msg = LinearCmd.Create(cmds);
            msg.DeviceIndex = Index;
            await _handler.SendMessageExpectOk(msg).ConfigureAwait(false);
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

            var result = await _handler.SendMessageAsync(new SensorReadCmd(Index, SensorReadAttributes(SensorType.Battery).ElementAt(0).Index, SensorType.Battery)).ConfigureAwait(false);
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
            await _handler.SendMessageExpectOk(new StopDeviceCmd(Index)).ConfigureAwait(false);
        }
    }
}
