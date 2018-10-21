// <copyright file="ButtplugBluetoothDevice.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Buttplug.Core.Devices;
using Buttplug.Core.Logging;
using JetBrains.Annotations;

namespace Buttplug.Server.Bluetooth
{
    public class ButtplugBluetoothDevice : ButtplugDevice
    {
        [NotNull]
        protected readonly IBluetoothDeviceInterface Interface;

        [NotNull]
        protected readonly IBluetoothDeviceInfo Info;

        protected ButtplugBluetoothDevice([NotNull] IButtplugLogManager aLogManager,
            [NotNull] string aName,
            [NotNull] IBluetoothDeviceInterface aInterface,
            [NotNull] IBluetoothDeviceInfo aInfo)
            : base(aLogManager,
                   aName,
                   aInterface.Address.ToString())
        {
            Interface = aInterface;
            Info = aInfo;
            Interface.DeviceRemoved += DeviceRemovedHandler;
        }

        public override void Disconnect()
        {
            Interface.Disconnect();
        }

        private void DeviceRemovedHandler(object aObject, EventArgs aEvent)
        {
            InvokeDeviceRemoved();
            Interface.DeviceRemoved -= DeviceRemovedHandler;
        }
    }
}
