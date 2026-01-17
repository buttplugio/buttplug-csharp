// <copyright file="DeviceMessageAttributes.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;

namespace Buttplug.Core.Messages
{
    /// <summary>
    /// Interface for messages containing Device Info, such as DeviceList.
    /// Updated for V4 protocol using DeviceFeatures instead of DeviceMessages.
    /// </summary>
    public interface IButtplugDeviceInfoMessage
    {
        /// <summary>
        /// Device name.
        /// </summary>
        string DeviceName { get; }

        /// <summary>
        /// Device index, as assigned by a Buttplug server.
        /// </summary>
        uint DeviceIndex { get; }

        /// <summary>
        /// User-configured display name for the device.
        /// </summary>
        string DeviceDisplayName { get; }

        /// <summary>
        /// Recommended time gap between messages in milliseconds.
        /// </summary>
        uint DeviceMessageTimingGap { get; }

        /// <summary>
        /// Device features, keyed by feature index as string.
        /// </summary>
        Dictionary<string, DeviceFeature> DeviceFeatures { get; }
    }
}
