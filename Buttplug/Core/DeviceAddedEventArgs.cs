using System;

namespace Buttplug.Core
{
    internal class DeviceAddedEventArgs : EventArgs
    {
        public ButtplugDevice Device { get; }

        public DeviceAddedEventArgs(ButtplugDevice d)
        {
            Device = d;
        }
    }
}
