using System;

namespace Buttplug.Core
{
    public class DeviceAddedEventArgs : EventArgs
    {
        public IButtplugDevice Device { get; }

        public DeviceAddedEventArgs(IButtplugDevice aDevice)
        {
            Device = aDevice;
        }
    }
}
