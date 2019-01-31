using System;
using Buttplug.Core.Logging;
using Buttplug.Devices;

namespace Buttplug.Test
{
    class TestDevice : ButtplugDevice
    {
        public TestDevice(IButtplugLogManager aManager, Type aProtocol, string aName)
        : base(aManager, aProtocol, new TestDeviceImpl(aManager, aName))
        {
        }

        public TestDevice(Type aProtocol, string aName)
            : base(new ButtplugLogManager(), aProtocol, new TestDeviceImpl(new ButtplugLogManager(), aName))
        {
        }

        public TestDevice(IButtplugLogManager aManager, string aName)
        : this(aManager, typeof(TestProtocol), aName)
        {
        }

        public TestDevice(string aName)
        : this(new ButtplugLogManager(), aName)
        {
        }

        public void RemoveDevice()
        {
            InvokeDeviceRemoved();
        }
    }
}
