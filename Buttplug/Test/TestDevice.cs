using Buttplug.Core.Logging;
using Buttplug.Devices;
using System;

namespace Buttplug.Test
{
    public class TestDevice : ButtplugDevice
    {
        public TestDevice(IButtplugLogManager aManager, Type aProtocol, string aName, string aAddress = null)
        : base(aManager, aProtocol, new TestDeviceImpl(aManager, aName, aAddress))
        {
        }

        public TestDevice(Type aProtocol, string aName, string aAddress = null)
        : base(new ButtplugLogManager(), aProtocol, new TestDeviceImpl(new ButtplugLogManager(), aName, aAddress))
        {
        }

        public TestDevice(IButtplugLogManager aManager, string aName, string aAddress = null)
        : this(aManager, typeof(TestProtocol), aName, aAddress)
        {
        }

        public TestDevice(string aName, string aAddress = null)
        : this(new ButtplugLogManager(), aName, aAddress)
        {
        }

        public void RemoveDevice()
        {
            _device.Disconnect();
            InvokeDeviceRemoved();
        }
    }
}
