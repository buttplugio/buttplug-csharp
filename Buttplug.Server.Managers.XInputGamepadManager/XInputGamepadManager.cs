using System;
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
            try
            {
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
            catch (DllNotFoundException e)
            {
                BpLogger.LogException(e, false, $"Required DirextX DLL not found: {e.Message}\nThis probably means you need to install the DirextX Runtimes from June 2010: https://www.microsoft.com/en-us/download/details.aspx?id=8109");
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