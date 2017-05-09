using Buttplug.Devices;
using SharpDX.XInput;
using System.Collections.Generic;

namespace Buttplug.Core
{
    internal class XInputGamepadManager : DeviceManager
    {
        private readonly List<XInputGamepadDevice> _connectedGamepads;

        public XInputGamepadManager()
        {
            _connectedGamepads = new List<XInputGamepadDevice>();
        }

        public override void StartScanning()
        {
            BpLogger.Trace("XInputGamepadManager start scanning");
            var controllers = new[] { new Controller(UserIndex.One),
                                      new Controller(UserIndex.Two),
                                      new Controller(UserIndex.Three),
                                      new Controller(UserIndex.Four) };
            foreach (var c in controllers)
            {
                if (!c.IsConnected)
                {
                    continue;
                }
                BpLogger.Debug($"Found connected XInput Gamepad for Index {c.UserIndex}");
                var device = new XInputGamepadDevice(c);
                _connectedGamepads.Add(device);
                InvokeDeviceAdded(new DeviceAddedEventArgs(device));
            }
        }

        public override void StopScanning()
        {
            // noop
            BpLogger.Trace("XInputGamepadManager stop scanning");
        }
    }
}