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

        /// <summary>
        /// The user-configured display name for the device.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Recommended time gap between messages in milliseconds.
        /// </summary>
        public uint MessageTimingGap { get; }

        /// <summary>
        /// The device features, keyed by feature index.
        /// </summary>
        public IReadOnlyDictionary<uint, ButtplugClientDeviceFeature> Features => _features;

        private readonly Dictionary<uint, ButtplugClientDeviceFeature> _features;
        private readonly ButtplugClientMessageHandler _handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="ButtplugClientDevice"/> class, using
        /// information received via a DeviceList message from the server.
        /// </summary>
        /// <param name="handler">Message handler for sending commands.</param>
        /// <param name="devInfo">Device info from the server.</param>
        internal ButtplugClientDevice(
            ButtplugClientMessageHandler handler,
            IButtplugDeviceInfoMessage devInfo)
            : this(handler, devInfo.DeviceIndex, devInfo.DeviceName, devInfo.DeviceFeatures, devInfo.DeviceDisplayName, devInfo.DeviceMessageTimingGap)
        {
            ButtplugUtils.ArgumentNotNull(devInfo, nameof(devInfo));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ButtplugClientDevice"/> class, using
        /// discrete parameters.
        /// </summary>
        /// <param name="handler">Message handler for sending commands.</param>
        /// <param name="index">The device index.</param>
        /// <param name="name">The device name.</param>
        /// <param name="deviceFeatures">The device features.</param>
        /// <param name="displayName">The user-configured display name.</param>
        /// <param name="messageTimingGap">Recommended time gap between messages.</param>
        internal ButtplugClientDevice(
            ButtplugClientMessageHandler handler,
            uint index,
            string name,
            Dictionary<string, DeviceFeature> deviceFeatures,
            string displayName,
            uint messageTimingGap)
        {
            ButtplugUtils.ArgumentNotNull(handler, nameof(handler));
            _handler = handler;
            Index = index;
            Name = name;
            DisplayName = displayName;
            MessageTimingGap = messageTimingGap;

            _features = new Dictionary<uint, ButtplugClientDeviceFeature>();
            if (deviceFeatures != null)
            {
                foreach (var kvp in deviceFeatures)
                {
                    if (uint.TryParse(kvp.Key, out var featureIndex))
                    {
                        _features[featureIndex] = new ButtplugClientDeviceFeature(this, kvp.Value, _handler);
                    }
                }
            }
        }

        #region Feature Queries

        /// <summary>
        /// Gets all features that have a specific output type.
        /// </summary>
        /// <param name="outputType">The output type to filter by.</param>
        /// <returns>Enumerable of matching features.</returns>
        public IEnumerable<ButtplugClientDeviceFeature> GetFeaturesWithOutput(OutputType outputType)
        {
            return _features.Values.Where(f => f.HasOutput(outputType));
        }

        /// <summary>
        /// Gets all features that have a specific input type.
        /// </summary>
        /// <param name="inputType">The input type to filter by.</param>
        /// <returns>Enumerable of matching features.</returns>
        public IEnumerable<ButtplugClientDeviceFeature> GetFeaturesWithInput(InputType inputType)
        {
            return _features.Values.Where(f => f.HasInput(inputType));
        }

        /// <summary>
        /// Gets a specific feature by its index.
        /// </summary>
        /// <param name="featureIndex">The feature index.</param>
        /// <returns>The feature, or null if not found.</returns>
        public ButtplugClientDeviceFeature GetFeature(uint featureIndex)
        {
            return _features.TryGetValue(featureIndex, out var feature) ? feature : null;
        }

        /// <summary>
        /// Checks if this device has any features with the specified output type.
        /// </summary>
        public bool HasOutput(OutputType outputType)
        {
            return _features.Values.Any(f => f.HasOutput(outputType));
        }

        /// <summary>
        /// Checks if this device has any features with the specified input type.
        /// </summary>
        public bool HasInput(InputType inputType)
        {
            return _features.Values.Any(f => f.HasInput(inputType));
        }

        #endregion

        #region Output Commands

        /// <summary>
        /// Sends an output command to all features that support the command's output type.
        /// </summary>
        /// <param name="command">The output command to send.</param>
        /// <param name="token">Cancellation token.</param>
        /// <example>
        /// <code>
        /// // Vibrate at 50%
        /// await device.RunOutputAsync(DeviceOutput.Vibrate.Percent(0.5));
        ///
        /// // Move to position over 500ms
        /// await device.RunOutputAsync(DeviceOutput.PositionWithDuration.Percent(1.0, 500));
        /// </code>
        /// </example>
        public async Task RunOutputAsync(DeviceOutputCommand command, CancellationToken token = default)
        {
            var features = GetFeaturesWithOutput(command.OutputType).ToList();
            if (!features.Any())
            {
                throw new ButtplugDeviceException($"Device {Name} has no features with output type {command.OutputType}");
            }

            var tasks = features.Select(f => f.RunOutputAsync(command, token));
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends an output command to a specific feature by index.
        /// </summary>
        /// <param name="featureIndex">The feature index.</param>
        /// <param name="command">The output command to send.</param>
        /// <param name="token">Cancellation token.</param>
        public async Task RunOutputAsync(uint featureIndex, DeviceOutputCommand command, CancellationToken token = default)
        {
            var feature = GetFeature(featureIndex);
            if (feature == null)
            {
                throw new ButtplugDeviceException($"Device {Name} does not have feature index {featureIndex}");
            }

            await feature.RunOutputAsync(command, token).ConfigureAwait(false);
        }

        #endregion

        #region Input Commands

        /// <summary>
        /// Sends an input command to the first feature that supports the command's input type.
        /// For Read commands, returns the reading. For Subscribe/Unsubscribe, returns null.
        /// </summary>
        /// <param name="command">The input command to send.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The input reading for Read commands, null for Subscribe/Unsubscribe.</returns>
        /// <example>
        /// <code>
        /// // Read battery level
        /// var reading = await device.RunInputAsync(DeviceInput.Battery.Read());
        /// var batteryLevel = reading?.GetValue(InputType.Battery);
        ///
        /// // Subscribe to button events
        /// await device.RunInputAsync(DeviceInput.Button.Subscribe());
        /// </code>
        /// </example>
        public async Task<InputReading> RunInputAsync(DeviceInputCommand command, CancellationToken token = default)
        {
            var features = GetFeaturesWithInput(command.InputType).ToList();
            if (!features.Any())
            {
                throw new ButtplugDeviceException($"Device {Name} has no features with input type {command.InputType}");
            }

            // Use the first feature that supports this input type
            return await features.First().RunInputAsync(command, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends an input command to a specific feature by index.
        /// </summary>
        /// <param name="featureIndex">The feature index.</param>
        /// <param name="command">The input command to send.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The input reading for Read commands, null for Subscribe/Unsubscribe.</returns>
        public async Task<InputReading> RunInputAsync(uint featureIndex, DeviceInputCommand command, CancellationToken token = default)
        {
            var feature = GetFeature(featureIndex);
            if (feature == null)
            {
                throw new ButtplugDeviceException($"Device {Name} does not have feature index {featureIndex}");
            }

            return await feature.RunInputAsync(command, token).ConfigureAwait(false);
        }

        #endregion

        #region Convenience Methods

        /// <summary>
        /// Reads the battery level of the device.
        /// </summary>
        /// <param name="timeout">Optional timeout for the operation.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Battery level as a value between 0.0 and 1.0.</returns>
        /// <exception cref="ButtplugDeviceException">Thrown if the device does not support battery reading.</exception>
        /// <exception cref="ButtplugMessageException">Thrown if the battery reading response is invalid.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation times out or is cancelled.</exception>
        public async Task<double> BatteryAsync(TimeSpan? timeout = null, CancellationToken token = default)
        {
            CancellationTokenSource linkedCts = null;
            try
            {
                var effectiveToken = token;
                if (timeout.HasValue)
                {
                    linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token);
                    linkedCts.CancelAfter(timeout.Value);
                    effectiveToken = linkedCts.Token;
                }

                var reading = await RunInputAsync(DeviceInput.Battery.Read(), effectiveToken).ConfigureAwait(false);
                var batteryValue = reading?.GetValue(InputType.Battery);
                if (!batteryValue.HasValue)
                {
                    throw new ButtplugMessageException($"Battery reading from device '{Name}' did not contain a battery value.");
                }

                // Battery is typically returned as 0-100, convert to 0.0-1.0
                return batteryValue.Value / 100.0;
            }
            finally
            {
                linkedCts?.Dispose();
            }
        }

        /// <summary>
        /// Stops all actions on this device.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        public async Task StopAsync(CancellationToken token = default)
        {
            await _handler.SendMessageExpectOk(new StopDeviceCmd(Index), token).ConfigureAwait(false);
        }

        #endregion
    }
}
