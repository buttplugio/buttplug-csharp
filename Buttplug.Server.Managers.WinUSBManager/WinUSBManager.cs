// <copyright file="WinUSBManager.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Buttplug.Core.Logging;
using MadWizard.WinUSBNet;
using Microsoft.Win32;

namespace Buttplug.Server.Managers.WinUSBManager
{
    public class WinUSBManager : DeviceSubtypeManager
    {
        public WinUSBManager(IButtplugLogManager aLogManager)
            : base(aLogManager)
        {
            BpLogger.Info("Loading WinUSB Manager");
        }

        public override void StartScanning()
        {
            BpLogger.Info("WinUSBManager start scanning");
            var deviceKey =
                Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USB\VID_0B49&PID_064F");
            if (deviceKey == null)
            {
                BpLogger.Debug("No TranceVibrator Devices found in registry.");
                InvokeScanningFinished();
                return;
            }

            var deviceKeyNames = deviceKey.GetSubKeyNames();
            if (deviceKeyNames.Length == 0)
            {
                BpLogger.Debug("No TranceVibrator Devices with drivers found in registry.");
                InvokeScanningFinished();
                return;
            }

            var deviceSubKey = deviceKey.OpenSubKey(deviceKeyNames[0]);
            if (deviceSubKey == null)
            {
                BpLogger.Debug("No TranceVibrator Devices with drivers subkeys found in registry.");
                InvokeScanningFinished();
                return;
            }

            var deviceParameters = deviceSubKey.OpenSubKey("Device Parameters");
            if (deviceParameters == null)
            {
                BpLogger.Debug("No TranceVibrator Devices with drivers parameters found in registry.");
                InvokeScanningFinished();
                return;
            }

            var deviceRegistryObject = deviceParameters.GetValue("DeviceInterfaceGUIDs", string.Empty);
            string deviceGuid = string.Empty;
            if (deviceRegistryObject == null)
            {
                BpLogger.Debug("No TranceVibrator Devices Driver GUIDs found in registry.");
                InvokeScanningFinished();
                return;
            }

            try
            {
                var guidStrings = (string[])deviceRegistryObject;
                deviceGuid = guidStrings[0];
            }
            catch (Exception)
            {
                try
                {
                    // Some versions of Zadig, the registry key writes as a string, not a string array?
                    deviceGuid = (string)deviceRegistryObject;
                }
                catch (Exception)
                {
                    BpLogger.Error("Cannot cast device GUID from registry value, cannot connect to Trancevibe.");
                }
            }

            if (deviceGuid.Length == 0)
            {
                BpLogger.Error("Cannot find device GUID from registry value, cannot connect to Trancevibe.");
                return;
            }

            // Only valid for our Trancevibrator install
            var devices = USBDevice.GetDevices(deviceGuid);
            if (devices == null || devices.Length == 0)
            {
                BpLogger.Error("No USB Device found!");
                InvokeScanningFinished();
                return;
            }

            uint index = 0;
            foreach (var deviceinfo in devices)
            {
                var device = new USBDevice(deviceinfo);
                BpLogger.Debug("Found TranceVibrator Device");
                var tvDevice = new RezTranceVibratorDevice(LogManager, device, index);
                index += 1;
                InvokeDeviceAdded(new DeviceAddedEventArgs(tvDevice));
            }

            InvokeScanningFinished();
        }

        public override void StopScanning()
        {
            // noop
        }

        public override bool IsScanning()
        {
            // noop
            return false;
        }
    }
}
