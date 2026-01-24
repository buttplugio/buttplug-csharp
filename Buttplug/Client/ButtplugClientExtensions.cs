// <copyright file="ButtplugClientExtensions.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Messages;

namespace Buttplug.Client
{
    /// <summary>
    /// Extension methods for <see cref="ButtplugClient"/> providing convenient shortcuts for common operations.
    /// </summary>
    public static class ButtplugClientExtensions
    {
        /// <summary>
        /// Connects to a Buttplug server using a WebSocket URI string.
        /// </summary>
        /// <param name="client">The client to connect.</param>
        /// <param name="uri">The WebSocket URI (e.g., "ws://localhost:12345").</param>
        /// <param name="token">Cancellation token.</param>
        /// <example>
        /// <code>
        /// var client = new ButtplugClient("My App");
        /// await client.ConnectAsync("ws://localhost:12345");
        /// </code>
        /// </example>
        public static Task ConnectAsync(this ButtplugClient client, string uri, CancellationToken token = default)
        {
            return client.ConnectAsync(new Uri(uri), token);
        }

        /// <summary>
        /// Connects to a Buttplug server using a WebSocket URI.
        /// </summary>
        /// <param name="client">The client to connect.</param>
        /// <param name="uri">The WebSocket URI.</param>
        /// <param name="token">Cancellation token.</param>
        public static Task ConnectAsync(this ButtplugClient client, Uri uri, CancellationToken token = default)
        {
            return client.ConnectAsync(new ButtplugWebsocketConnector(uri), token);
        }

        /// <summary>
        /// Gets a device by its index.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="index">The device index.</param>
        /// <returns>The device if found, otherwise null.</returns>
        public static ButtplugClientDevice GetDevice(this ButtplugClient client, uint index)
        {
            return client.Devices.FirstOrDefault(d => d.Index == index);
        }

        /// <summary>
        /// Gets all devices that have the specified output type.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="outputType">The output type to filter by.</param>
        /// <returns>Enumerable of devices with the specified output type.</returns>
        public static IEnumerable<ButtplugClientDevice> GetDevicesWithOutput(this ButtplugClient client, OutputType outputType)
        {
            return client.Devices.Where(d => d.HasOutput(outputType));
        }

        /// <summary>
        /// Gets all devices that have the specified input type.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="inputType">The input type to filter by.</param>
        /// <returns>Enumerable of devices with the specified input type.</returns>
        public static IEnumerable<ButtplugClientDevice> GetDevicesWithInput(this ButtplugClient client, InputType inputType)
        {
            return client.Devices.Where(d => d.HasInput(inputType));
        }

        /// <summary>
        /// Sends a vibrate command to all devices that support vibration.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="percent">The vibration intensity (0.0 to 1.0).</param>
        /// <param name="token">Cancellation token.</param>
        /// <example>
        /// <code>
        /// await client.VibrateAllAsync(0.5); // Vibrate all devices at 50%
        /// </code>
        /// </example>
        public static async Task VibrateAllAsync(this ButtplugClient client, double percent, CancellationToken token = default)
        {
            var devices = client.GetDevicesWithOutput(OutputType.Vibrate).ToList();
            if (!devices.Any())
            {
                return;
            }

            var tasks = devices.Select(d => d.RunOutputAsync(DeviceOutput.Vibrate.Percent(percent), token));
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends an oscillate command to all devices that support oscillation.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="percent">The oscillation intensity (0.0 to 1.0).</param>
        /// <param name="token">Cancellation token.</param>
        public static async Task OscillateAllAsync(this ButtplugClient client, double percent, CancellationToken token = default)
        {
            var devices = client.GetDevicesWithOutput(OutputType.Oscillate).ToList();
            if (!devices.Any())
            {
                return;
            }

            var tasks = devices.Select(d => d.RunOutputAsync(DeviceOutput.Oscillate.Percent(percent), token));
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends a rotate command to all devices that support rotation.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="percent">The rotation speed (0.0 to 1.0).</param>
        /// <param name="token">Cancellation token.</param>
        public static async Task RotateAllAsync(this ButtplugClient client, double percent, CancellationToken token = default)
        {
            var devices = client.GetDevicesWithOutput(OutputType.Rotate).ToList();
            if (!devices.Any())
            {
                return;
            }

            var tasks = devices.Select(d => d.RunOutputAsync(DeviceOutput.Rotate.Percent(percent), token));
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends a position w/ duration command to all devices that support that output type.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="percent">The target position (0.0 to 1.0).</param>
        /// <param name="durationMs">The duration in milliseconds to reach the position.</param>
        /// <param name="token">Cancellation token.</param>
        public static async Task PositionWithDurationAllAsync(this ButtplugClient client, double percent, uint durationMs, CancellationToken token = default)
        {
            var devices = client.GetDevicesWithOutput(OutputType.HwPositionWithDuration).ToList();
            if (!devices.Any())
            {
                return;
            }

            var tasks = devices.Select(d => d.RunOutputAsync(DeviceOutput.PositionWithDuration.Percent(percent, durationMs), token));
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }
}
