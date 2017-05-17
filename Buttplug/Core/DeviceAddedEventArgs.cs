using System;

namespace Buttplug.Core
{
    public class DeviceAddedEventArgs : EventArgs
    {
        public ButtplugDevice Device { get; }

        public DeviceAddedEventArgs(ButtplugDevice d)
        {
            Device = d;
        }
    }
}
