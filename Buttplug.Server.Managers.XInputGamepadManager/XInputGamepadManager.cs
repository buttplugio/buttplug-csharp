// <copyright file="XInputGamepadManager.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Buttplug.Core.Logging;
using Buttplug.Devices;
using Buttplug.Devices.Protocols;
using SharpDX.XInput;

namespace Buttplug.Server.Managers.XInputGamepadManager
{
    public class XInputGamepadManager : DeviceSubtypeManager
    {
        public XInputGamepadManager(IButtplugLogManager aLogManager)
            : base(aLogManager)
        {
            BpLogger.Info("Loading XInput Gamepad Manager");
        }

        public override void StartScanning()
        {
            BpLogger.Info("XInputGamepadManager start scanning");
            try
            {
                // TODO this should scan in a loop on a timer until told to stop
                var controllers = new[]
                {
                    new Controller(UserIndex.One),
                    new Controller(UserIndex.Two),
                    new Controller(UserIndex.Three),
                    new Controller(UserIndex.Four),
                };
                foreach (var c in controllers)
                {
                    if (!c.IsConnected)
                    {
                        continue;
                    }

                    BpLogger.Debug($"Found connected XInput Gamepad for Index {c.UserIndex}");
                    var deviceImpl = new XInputGamepadDevice(LogManager, c);
                    var device = new ButtplugDevice(LogManager, new XInputProtocol(LogManager, deviceImpl), deviceImpl);
                    InvokeDeviceAdded(new DeviceAddedEventArgs(device));
                    InvokeScanningFinished();
                }
            }
            catch (DllNotFoundException e)
            {
                // TODO Should we maybe try testing for this in construction instead of during scanning?
                BpLogger.Error($"Required DirectX DLL not found: {e.Message}\nThis probably means you need to install the DirectX Runtime from June 2010: https://www.microsoft.com/en-us/download/details.aspx?id=8109");
                InvokeScanningFinished();
            }
        }

        public override void StopScanning()
        {
            // noop
            BpLogger.Info("XInputGamepadManager stop scanning");
        }

        public override bool IsScanning()
        {
            // noop
            return false;
        }
    }
}