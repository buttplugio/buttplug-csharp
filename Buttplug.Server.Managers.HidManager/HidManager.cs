// <copyright file="HidManager.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Buttplug.Core.Logging;
using Buttplug.Server.Managers.HidManager.Devices;
using HidLibrary;

namespace Buttplug.Server.Managers.HidManager
{
    public class HidManager : DeviceSubtypeManager
    {
        private readonly List<HidDeviceFactory> _deviceFactories;

        private readonly IButtplugLog _log;

        private bool _scanning;

        public HidManager(IButtplugLogManager aLogger)
            : base(aLogger)
        {
            _deviceFactories = new List<HidDeviceFactory>() { new HidDeviceFactory(aLogger, new CycloneX10HidDeviceInfo()) };
        }

        public override void StartScanning()
        {
            _scanning = true;
            var hidDevices = new HidEnumerator();
            foreach (var hid in hidDevices.Enumerate())
            {
                try
                {
                    hid.ReadProduct(out var product);
                    hid.ReadManufacturer(out var vendor);
                    var prod = Encoding.Unicode.GetString(product);
                    var vend = Encoding.Unicode.GetString(vendor);
                    prod = prod.Substring(0, prod.IndexOf('\0'));
                    vend = vend.Substring(0, vend.IndexOf('\0'));

                    BpLogger.Trace("Found HID device (" +
                        hid.Attributes.VendorHexId + ":" + hid.Attributes.ProductHexId +
                        "): " + vend + " - " + prod);

                    var factories = _deviceFactories.Where(x =>
                        x.MayBeDevice(hid.Attributes.VendorId, hid.Attributes.ProductId));
                    var buttplugHidDeviceFactories = factories as HidDeviceFactory[] ?? factories.ToArray();
                    if (buttplugHidDeviceFactories.Length != 1)
                    {
                        if (buttplugHidDeviceFactories.Any())
                        {
                            BpLogger.Warn($"Found multiple HID factories for {hid.Attributes.VendorHexId}:{hid.Attributes.ProductHexId}");
                            buttplugHidDeviceFactories.ToList().ForEach(x => BpLogger.Warn(x.GetType().Name));
                        }
                        else
                        {
                            // BpLogger.Trace("No BLE factories found for device.");
                        }

                        continue;
                    }

                    var factory = buttplugHidDeviceFactories.First();
                    BpLogger.Debug($"Found HID factory: {factory.GetType().Name}");

                    var d = factory.CreateDevice(hid);
                    InvokeDeviceAdded(new DeviceAddedEventArgs(d));
                }
                catch (Exception e)
                {
                    // TODO Figure out what exceptions can actually be thrown here.
                    BpLogger.Error(e.Message);
                }
            }

            _scanning = false;
            InvokeScanningFinished();
        }

        public override void StopScanning()
        {
        }

        public override bool IsScanning()
        {
            return _scanning;
        }
    }
}