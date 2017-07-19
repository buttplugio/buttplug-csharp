using Buttplug.Core;
using SharpDX.XInput;

namespace Buttplug.Server.Managers.XInputGamepadManager
{
    public class XInputGamepadManager : DeviceSubtypeManager
    {
        public XInputGamepadManager(IButtplugLogManager aLogManager)
            : base(aLogManager)
        {
            BpLogger.Info("Loading XInput Gamepad Manager");
        }

        public override void StartScanning()
        {
            BpLogger.Info("XInputGamepadManager start scanning");
            var controllers = new[]
            {
                new Controller(UserIndex.One),
                new Controller(UserIndex.Two),
                new Controller(UserIndex.Three),
                new Controller(UserIndex.Four),
            };
            foreach (var c in controllers)
            {
                if (!c.IsConnected)
                {
                    continue;
                }

                BpLogger.Debug($"Found connected XInput Gamepad for Index {c.UserIndex}");
                var device = new XInputGamepadDevice(LogManager, c);
                InvokeDeviceAdded(new DeviceAddedEventArgs(device));
                InvokeScanningFinished();
            }
        }

        public override void StopScanning()
        {
            // noop
            BpLogger.Info("XInputGamepadManager stop scanning");
        }

        public override bool IsScanning()
        {
            // noop
            return false;
        }
    }
}