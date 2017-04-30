using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Gaming.Input;
using Buttplug.Devices;

namespace Buttplug
{
    class UWPGamepadManager : DeviceManager
    {
        //TODO Pay attention to gamepad events
        List<UWPGamepadDevice> ConnectedGamepads;

        public UWPGamepadManager()
        {
            ConnectedGamepads = new List<UWPGamepadDevice>();
            Gamepad.GamepadAdded += GamepadAdded;
        }

        public override void StartScanning()
        {
            //Noop
        }

        public void GamepadAdded(object o, Gamepad e)
        {
            var device = new UWPGamepadDevice(e);
            ConnectedGamepads.Add(device);
            InvokeDeviceAdded(new DeviceAddedEventArgs(device));
        }

        public override void StopScanning()
        {
            // noop
        }
        
    }
}
