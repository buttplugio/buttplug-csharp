// <copyright file="IHidDeviceInfo.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using Buttplug.Core.Devices;
using Buttplug.Core.Logging;
using HidLibrary;

namespace Buttplug.Server.Managers.HidManager
{
    public interface IHidDeviceInfo
    {
        string Name { get; }

        int ProductId { get; }

        int VendorId { get; }

        IButtplugDevice CreateDevice(IButtplugLogManager buttplugLogManager, IHidDevice aHid);
    }
}