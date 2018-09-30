// <copyright file="ButtplugSerialDeviceFactory.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using Buttplug.Core.Devices;
using Buttplug.Core.Logging;

namespace Buttplug.Server.Managers.SerialPortManager
{
    public abstract class ButtplugSerialDeviceFactory
    {
        public abstract IButtplugDevice CreateDevice(IButtplugLogManager aLogManager, string aPortName);
    }
}
