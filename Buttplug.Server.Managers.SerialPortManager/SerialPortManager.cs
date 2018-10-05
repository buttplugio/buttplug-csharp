// <copyright file="SerialPortManager.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
using Buttplug.Core.Logging;

namespace Buttplug.Server.Managers.SerialPortManager
{
    public class SerialPortManager : DeviceSubtypeManager
    {
        private Dictionary<string, ButtplugSerialDeviceFactory> _portDeviceTypeMap = new Dictionary<string, ButtplugSerialDeviceFactory>();
        private bool _isScanning;

        public SerialPortManager(IButtplugLogManager aLogManager)
            : base(aLogManager)
        {
            BpLogger.Info("Loading ErosTek Serial Port Manager");
        }

        public void AddPortProtocolMapping(string aPortName, ButtplugSerialDeviceFactory aDeviceType)
        {
            _portDeviceTypeMap.Add(aPortName, aDeviceType);
        }

        public override void StartScanning()
        {
            BpLogger.Info("Starting Scanning Serial Ports for ErosTek Devices");
            _isScanning = true;
            foreach (var entry in _portDeviceTypeMap)
            {
                var device = entry.Value.CreateDevice(LogManager, entry.Key);
                if (device == null)
                {
                    BpLogger.Error($"Cannot open device on port {entry.Key}.");
                    continue;
                }

                device.InitializeAsync().Wait();
            }
        }

        public override void StopScanning()
        {
            BpLogger.Info("Stopping Scanning Serial Ports for ErosTek Devices");
            _isScanning = false;
        }

        public override bool IsScanning()
        {
            return _isScanning;
        }
    }
}