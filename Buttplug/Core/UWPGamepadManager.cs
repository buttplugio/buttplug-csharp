using Buttplug.Devices;
using System.Collections.Generic;
using Windows.Gaming.Input;
using Buttplug.Logging;

namespace Buttplug.Core
{
    internal class UWPGamepadManager : DeviceSubtypeManager
    {
        //TODO Pay attention to gamepad events
        private readonly List<UwpGamepadDevice> _connectedGamepads;

        public UWPGamepadManager()
        {
            _connectedGamepads = new List<UwpGamepadDevice>();
            Gamepad.GamepadAdded += GamepadAdded;
        }

        public override void StartScanning()
        {
            //Noop
            BpLogger.Trace("UWPGamepadManager start scanning");
        }

        public void GamepadAdded(object o, Gamepad e)
        {
            BpLogger.Trace("UWPGamepadManager GamepadAdded");
            var device = new UwpGamepadDevice(e);
            _connectedGamepads.Add(device);
            InvokeDeviceAdded(new DeviceAddedEventArgs(device));
        }

        public override void StopScanning()
        {
            // noop
            BpLogger.Trace("UWPGamepadManager stop scanning");
        }
    }
}