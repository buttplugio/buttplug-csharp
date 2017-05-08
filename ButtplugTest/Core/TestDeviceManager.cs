using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buttplug.Core;

namespace ButtplugTest.Core
{
    class TestDeviceManager : DeviceManager
    {
        private TestDevice _device;

        public TestDeviceManager(TestDevice aDevice)
        {
            _device = aDevice;
        }

        public override void StartScanning()
        {
            this.InvokeDeviceAdded(new DeviceAddedEventArgs(_device));
        }

        public override void StopScanning()
        {
            
        }
    }
}
