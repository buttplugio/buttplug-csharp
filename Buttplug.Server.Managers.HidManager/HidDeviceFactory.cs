// <copyright file="HidDeviceFactory.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using Buttplug.Core.Devices;
using Buttplug.Core.Logging;
using HidLibrary;

namespace Buttplug.Server.Managers.HidManager
{
    internal class HidDeviceFactory
    {
        private readonly IButtplugLog _bpLogger;

        private readonly IHidDeviceInfo _deviceInfo;

        private readonly IButtplugLogManager _buttplugLogManager;

        public HidDeviceFactory(IButtplugLogManager aLogManager, IHidDeviceInfo aInfo)
        {
            _buttplugLogManager = aLogManager;
            _bpLogger = _buttplugLogManager.GetLogger(GetType());
            _bpLogger.Trace($"Creating {GetType().Name}");
            _deviceInfo = aInfo;
        }

        public bool MayBeDevice(int aVendorId, int aProductId)
        {
            if (_deviceInfo.VendorId != aVendorId || _deviceInfo.ProductId != aProductId)
            {
                return false;
            }

            _bpLogger.Debug("Matched " + _deviceInfo.Name);
            return true;
        }

        internal IButtplugDevice CreateDevice(IHidDevice aHid)
        {
            return _deviceInfo.CreateDevice(_buttplugLogManager, aHid);
        }
    }
}
