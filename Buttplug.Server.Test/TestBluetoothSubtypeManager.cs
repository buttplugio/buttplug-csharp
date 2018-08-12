// <copyright file="TestBluetoothSubtypeManager.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
using Buttplug.Core;
using Buttplug.Server.Bluetooth;

namespace Buttplug.Server.Test
{
    internal class TestBluetoothSubtypeManager : BluetoothSubtypeManager
    {
        public TestBluetoothSubtypeManager(IButtplugLogManager aLogManager)
            : base(aLogManager)
        {
        }

        public List<IBluetoothDeviceInfo> GetDefaultDeviceInfoList()
        {
            return BuiltinDevices;
        }

        public override void StartScanning()
        {
        }

        public override void StopScanning()
        {
        }

        public override bool IsScanning()
        {
            return false;
        }
    }
}
