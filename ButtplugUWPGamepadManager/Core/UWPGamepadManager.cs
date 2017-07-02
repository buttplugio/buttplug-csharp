using System.Collections.Generic;
using Windows.Gaming.Input;
using Buttplug.Core;
using ButtplugUWPGamepadManager.Devices;

namespace ButtplugUWPGamepadManager.Core
{
    internal class UWPGamepadManager : DeviceSubtypeManager
    {
        //TODO Pay attention to gamepad events
        // ReSharper disable once CollectionNeverQueried.Local
        private readonly List<UwpGamepadDevice> _connectedGamepads;

        public UWPGamepadManager(IButtplugLogManager aLogManager) : base(aLogManager)
        {
            _connectedGamepads = new List<UwpGamepadDevice>();
            Gamepad.GamepadAdded += GamepadAdded;
        }

        public override void StartScanning()
        {
            //Noop
            BpLogger.Trace("UWPGamepadManager start scanning");
        }

        private void GamepadAdded(object aObj, Gamepad aEvent)
        {
            BpLogger.Trace("UWPGamepadManager GamepadAdded");
            var device = new UwpGamepadDevice(LogManager, aEvent);
            _connectedGamepads.Add(device);
            InvokeDeviceAdded(new DeviceAddedEventArgs(device));
            InvokeScanningFinished();
        }

        public override void StopScanning()
        {
            // noop
            BpLogger.Trace("UWPGamepadManager stop scanning");
        }

        public override bool IsScanning()
        {
            // noop
            return false;
        }
    }
}