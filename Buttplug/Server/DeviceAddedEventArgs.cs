// <copyright file="DeviceAddedEventArgs.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Buttplug.Core.Devices;

namespace Buttplug.Server
{
    public class DeviceAddedEventArgs : EventArgs
    {
        public IButtplugDevice Device { get; }

        public DeviceAddedEventArgs(IButtplugDevice aDevice)
        {
            Device = aDevice;
        }
    }
}
