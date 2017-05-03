using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.XInput;
using Buttplug.Devices;

namespace Buttplug
{
    class XInputGamepadManager : DeviceManager
    {
        List<XInputGamepadDevice> ConnectedGamepads;

        public XInputGamepadManager()
        {
            ConnectedGamepads = new List<XInputGamepadDevice>();
        }

        public override void StartScanning()
        {
            BPLogger.Trace("XInputGamepadManager start scanning");
            var controllers = new[] { new Controller(UserIndex.One),
                                      new Controller(UserIndex.Two),
                                      new Controller(UserIndex.Three),
                                      new Controller(UserIndex.Four) };
            foreach (var c in controllers)
            {
                if (c.IsConnected)
                {
                    BPLogger.Debug($"Found connected XInput Gamepad for Index {c.UserIndex}");
                    var device = new XInputGamepadDevice(c);
                    ConnectedGamepads.Add(device);
                    InvokeDeviceAdded(new DeviceAddedEventArgs(device));
                }
            }
        }

        public override void StopScanning()
        {
            // noop
            BPLogger.Trace("XInputGamepadManager stop scanning");
        }

    }
}
