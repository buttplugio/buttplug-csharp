using System.Collections.Generic;
using Buttplug.Core;
using ButtplugXInputGamepadManager.Devices;
using SharpDX.XInput;

namespace ButtplugXInputGamepadManager.Core
{
    public class XInputGamepadManager : DeviceSubtypeManager
    {
        private readonly List<XInputGamepadDevice> _connectedGamepads;

        public XInputGamepadManager(IButtplugLogManager aLogManager) : base(aLogManager)
        {
            BpLogger.Debug("Loading XInput Gamepad Manager");
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
                var device = new XInputGamepadDevice(LogManager, c);
                _connectedGamepads.Add(device);
                InvokeDeviceAdded(new DeviceAddedEventArgs(device));
                InvokeScanningFinished();
            }
        }

        public override void StopScanning()
        {
            // noop
            BpLogger.Trace("XInputGamepadManager stop scanning");
        }

        public override bool IsScanning()
        {
            // noop
            return false;
        }
    }
}