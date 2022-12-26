// <copyright file="ButtplugDeviceMessage.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Newtonsoft.Json;

namespace Buttplug.Core.Messages
{
    /// <summary>
    /// Subclass of Buttplug Messages, that command a device to take an action.
    /// </summary>
    public class ButtplugDeviceMessage : ButtplugMessage
    {
        /// <summary>
        /// Device index the message is intended for.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public uint DeviceIndex { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ButtplugDeviceMessage"/> class.
        /// </summary>
        /// <param name="id">Message ID.</param>
        /// <param name="deviceIndex">Device index.</param>
        public ButtplugDeviceMessage(uint id = ButtplugConsts.DefaultMsgId, uint deviceIndex = uint.MaxValue)
            : base(id)
        {
            DeviceIndex = deviceIndex;
        }
    }
}