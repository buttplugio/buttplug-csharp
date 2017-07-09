using Buttplug.Core;
using Buttplug.Server;
using JetBrains.Annotations;

namespace Buttplug.Server.Test
{
    internal class TestDeviceSubtypeManager : DeviceSubtypeManager
    {
        [CanBeNull]
        private readonly TestDevice _device;

        public bool StartScanningCalled { get; private set; }

        public bool StopScanningCalled { get; private set; }

        public TestDeviceSubtypeManager([NotNull] IButtplugLogManager aLogManager)
            : base(aLogManager)
        {
        }

        public TestDeviceSubtypeManager([NotNull] IButtplugLogManager aLogManager, [NotNull] TestDevice aDevice)
            : base(aLogManager)
        {
            _device = aDevice;
        }

        public override void StartScanning()
        {
            StartScanningCalled = true;
            StopScanningCalled = false;
            if (!(_device is null))
            {
                InvokeDeviceAdded(new DeviceAddedEventArgs(_device));
            }
        }

        public override void StopScanning()
        {
            StopScanningCalled = true;
            StartScanningCalled = false;
            InvokeScanningFinished();
        }

        public override bool IsScanning()
        {
            return StartScanningCalled && !StopScanningCalled;
        }
    }
}