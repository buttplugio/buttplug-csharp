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
        }

        public override void StartScanning()
        {
            Console.WriteLine("Number of gamepads: " + Gamepad.Gamepads.Count());
            var newGamepads = Gamepad.Gamepads;
            foreach (var g in newGamepads)
            {
                var d = from x in ConnectedGamepads
                        where x.Device == g
                        select x;
                if (!d.Any())
                {
                    var device = new GamepadDevice(g);
                    ConnectedGamepads.Add(device);
                    InvokeDeviceAdded(new DeviceAddedEventArgs(device));
                }
            }
        }

        public void GamepadAdded(object o, Gamepad e)
        {
            Console.WriteLine("Found gamepad!");
        }

        public override void StopScanning()
        {
            // noop
        }
        
    }
}
