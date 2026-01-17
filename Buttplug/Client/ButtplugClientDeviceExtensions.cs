// <copyright file="ButtplugClientDeviceExtensions.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Messages;

namespace Buttplug.Client
{
    /// <summary>
    /// Extension methods for <see cref="ButtplugClientDevice"/> providing convenient shortcuts for common operations.
    /// </summary>
    public static class ButtplugClientDeviceExtensions
    {
        /// <summary>
        /// Sends a vibrate command to all vibration features on this device.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="percent">The vibration intensity (0.0 to 1.0).</param>
        /// <param name="token">Cancellation token.</param>
        /// <example>
        /// <code>
        /// await device.VibrateAsync(0.5); // Vibrate at 50%
        /// </code>
        /// </example>
        public static Task VibrateAsync(this ButtplugClientDevice device, double percent, CancellationToken token = default)
        {
            return device.RunOutputAsync(DeviceOutput.Vibrate.Percent(percent), token);
        }

        /// <summary>
        /// Sends an oscillate command to all oscillation features on this device.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="percent">The oscillation intensity (0.0 to 1.0).</param>
        /// <param name="token">Cancellation token.</param>
        public static Task OscillateAsync(this ButtplugClientDevice device, double percent, CancellationToken token = default)
        {
            return device.RunOutputAsync(DeviceOutput.Oscillate.Percent(percent), token);
        }

        /// <summary>
        /// Sends a rotate command to all rotation features on this device.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="percent">The rotation speed (0.0 to 1.0).</param>
        /// <param name="token">Cancellation token.</param>
        public static Task RotateAsync(this ButtplugClientDevice device, double percent, CancellationToken token = default)
        {
            return device.RunOutputAsync(DeviceOutput.Rotate.Percent(percent), token);
        }

        /// <summary>
        /// Sends a position w/ duration command to all supported features on this device.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="percent">The target position (0.0 to 1.0).</param>
        /// <param name="durationMs">The duration in milliseconds to reach the position.</param>
        /// <param name="token">Cancellation token.</param>
        /// <example>
        /// <code>
        /// await device.PositionWithDurationAsync(1.0, 500); // Move to top over 500ms
        /// await device.PositionWithDurationAsync(0.0, 500); // Move to bottom over 500ms
        /// </code>
        /// </example>
        public static Task PositionWithDurationAsync(this ButtplugClientDevice device, double percent, uint durationMs, CancellationToken token = default)
        {
            return device.RunOutputAsync(DeviceOutput.PositionWithDuration.Percent(percent, durationMs), token);
        }

        /// <summary>
        /// Sends a constrict command to all constriction features on this device.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="percent">The constriction level (0.0 to 1.0).</param>
        /// <param name="token">Cancellation token.</param>
        public static Task ConstrictAsync(this ButtplugClientDevice device, double percent, CancellationToken token = default)
        {
            return device.RunOutputAsync(DeviceOutput.Constrict.Percent(percent), token);
        }

        /// <summary>
        /// Sends an inflate command to all inflation features on this device.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="percent">The inflation level (0.0 to 1.0).</param>
        /// <param name="token">Cancellation token.</param>
        public static Task InflateAsync(this ButtplugClientDevice device, double percent, CancellationToken token = default)
        {
            return device.RunOutputAsync(DeviceOutput.Inflate.Percent(percent), token);
        }
    }
}
