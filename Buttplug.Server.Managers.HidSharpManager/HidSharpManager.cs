// <copyright file="HidSharpManager.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using Buttplug.Core.Logging;
using Buttplug.Devices.Configuration;
using HidSharp;

namespace Buttplug.Server.Managers.HidSharpManager
{
    public class HidSharpManager : TimedScanDeviceSubtypeManager
    {
        List<string> _connectedAddresses = new List<string>();

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

                // We're already connected, just keep going.
                if (_connectedAddresses.Contains(device.DevicePath))
                {
                    continue;
                }
                var bpDevice = factory.CreateDevice(LogManager, new HidSharpHidDeviceImpl(LogManager, device)).Result;
                _connectedAddresses.Add(bpDevice.Identifier);
                void Removed(object aObj, EventArgs aArgs)
                {
                    _connectedAddresses.Remove(bpDevice.Identifier);
                    bpDevice.DeviceRemoved -= Removed;
                }
                bpDevice.DeviceRemoved += Removed;
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
                // We're already connected, just keep going.
                if (_connectedAddresses.Contains(port.DevicePath))
                {
                    continue;
                }
                var config = new OpenConfiguration();
                config.SetOption(OpenOption.Exclusive, true);
                config.SetOption(OpenOption.Interruptible, true);
                config.SetOption(OpenOption.TimeoutIfInterruptible, 1000);
                config.SetOption(OpenOption.TimeoutIfTransient, 1000);

                if (!port.TryOpen(config, out var stream))
                {
                    continue;
                }

                var deviceConfig = factory.Config as SerialProtocolConfiguration;

                stream.BaudRate = (int)deviceConfig.BaudRate;
                stream.DataBits = (int)deviceConfig.DataBits;
                stream.StopBits = (int)deviceConfig.StopBits;
                stream.Parity = SerialParity.None;

                var bpDevice = factory.CreateDevice(LogManager, new HidSharpSerialDeviceImpl(LogManager, stream)).Result;
                _connectedAddresses.Add(bpDevice.Identifier);
                void Removed(object aObj, EventArgs aArgs)
                {
                    _connectedAddresses.Remove(bpDevice.Identifier);
                    bpDevice.DeviceRemoved -= Removed;
                }
                bpDevice.DeviceRemoved += Removed;
                InvokeDeviceAdded(new DeviceAddedEventArgs(bpDevice));
            }
        }
    }
}
