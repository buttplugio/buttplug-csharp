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
            BPLogger.Trace("UWPGamepadManager start scanning");
        }

        public void GamepadAdded(object o, Gamepad e)
        {
            BPLogger.Trace("UWPGamepadManager GamepadAdded");
            var device = new UWPGamepadDevice(e);
            ConnectedGamepads.Add(device);
            InvokeDeviceAdded(new DeviceAddedEventArgs(device));
        }

        public override void StopScanning()
        {
            // noop
            BPLogger.Trace("UWPGamepadManager stop scanning");
        }

    }
}
