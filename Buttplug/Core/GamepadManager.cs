using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Gaming.Input;
using Buttplug.Devices;

namespace Buttplug
{
    class GamepadManager : IDeviceManager
    {
        //TODO Pay attention to gamepad events
        List<GamepadDevice> ConnectedGamepads;

        public GamepadManager()
        {
            ConnectedGamepads = new List<GamepadDevice>();
            Gamepad.GamepadAdded += GamepadAdded;
        }

        public override void StartScanning()
        {
            //Noop
        }

        public void GamepadAdded(object o, Gamepad e)
        {
            Console.WriteLine("Found gamepad!");
            var device = new GamepadDevice(e);
            ConnectedGamepads.Add(device);
            InvokeDeviceAdded(new DeviceAddedEventArgs(device));
        }

        public override void StopScanning()
        {
            // noop
        }
        
    }
}
