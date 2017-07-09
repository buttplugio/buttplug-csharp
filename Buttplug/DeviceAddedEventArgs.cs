using System;
using Buttplug.Core;

namespace Buttplug.Server
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
