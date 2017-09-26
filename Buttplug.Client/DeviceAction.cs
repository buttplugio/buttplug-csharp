using JetBrains.Annotations;

namespace Buttplug.Client
{
    public class DeviceEventArgs
    {
        public enum DeviceAction
        {
            ADDED,
            REMOVED,
        }

        [NotNull]
        public readonly ButtplugClientDevice Device;

        [NotNull]
        public readonly DeviceAction Action;

        public DeviceEventArgs(ButtplugClientDevice aDevice, DeviceAction aAction)
        {
            Device = aDevice;
            Action = aAction;
        }
    }
}