using Buttplug.Core;
using LanguageExt;

namespace ButtplugTest.Core
{
    internal class TestDeviceManager : DeviceSubtypeManager
    {
        private Option<TestDevice> _device;
        public bool StartScanningCalled { get; private set; }
        public bool StopScanningCalled { get; private set; }

        public TestDeviceManager()
        {
            _device = new OptionNone();
        }

        public TestDeviceManager(TestDevice aDevice)
        {
            _device = aDevice;
        }

        public override void StartScanning()
        {
            StartScanningCalled = true;
            _device.IfSome(x => this.InvokeDeviceAdded(new DeviceAddedEventArgs(x)));
        }

        public override void StopScanning()
        {
            StopScanningCalled = true;
        }
    }
}