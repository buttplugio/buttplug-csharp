// <copyright file="HidSharpManager.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Buttplug.Core.Logging;
using Buttplug.Devices.Configuration;
using HidSharp;

namespace Buttplug.Server.Managers.HidSharpManager
{
    public class HidSharpManager : TimedScanDeviceSubtypeManager
    {
        public HidSharpManager(IButtplugLogManager aLogManager)
            : base(aLogManager)
        {
        }

        protected override void RunScan()
        {
            var devList = DeviceList.Local;
            var hidDevices = devList.GetHidDevices();
            var serialDevices = devList.GetSerialDevices();

            foreach (var device in hidDevices)
            {
                var hidFinder = new HIDProtocolConfiguration((ushort)device.VendorID, (ushort)device.ProductID);
                var factory = DeviceConfigurationManager.Manager.Find(hidFinder);
                if (factory == null)
                {
                    continue;
                }

                var bpDevice = factory.CreateDevice(LogManager, new HidSharpHidDeviceImpl(LogManager, device)).Result;
                InvokeDeviceAdded(new DeviceAddedEventArgs(bpDevice));
            }

            foreach (var port in serialDevices)
            {
                var serialFinder = new SerialProtocolConfiguration(port.GetFileSystemName());
                var factory = DeviceConfigurationManager.Manager.Find(serialFinder);
                if (factory == null)
                {
                    continue;
                }

                if (!port.TryOpen(out var stream))
                {
                    continue;
                }

                var deviceConfig = factory.Config as SerialProtocolConfiguration;

                stream.BaudRate = (int)deviceConfig.BaudRate;
                stream.DataBits = (int)deviceConfig.DataBits;
                stream.StopBits = (int)deviceConfig.StopBits;
                stream.Parity = SerialParity.None;

                var bpDevice = factory.CreateDevice(LogManager, new HidSharpSerialDeviceImpl(LogManager, stream)).Result;
                InvokeDeviceAdded(new DeviceAddedEventArgs(bpDevice));
            }
        }
    }
}
