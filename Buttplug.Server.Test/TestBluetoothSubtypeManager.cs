using System.Collections.Generic;
using Buttplug.Core;
using Buttplug.Server.Bluetooth;

namespace Buttplug.Server.Test
{
    internal class TestBluetoothSubtypeManager : BluetoothSubtypeManager
    {
        public TestBluetoothSubtypeManager(IButtplugLogManager aLogManager)
            : base(aLogManager)
        {
        }

        public List<IBluetoothDeviceInfo> GetDefaultDeviceInfoList()
        {
            return BuiltinDevices;
        }

        public override void StartScanning()
        {
        }

        public override void StopScanning()
        {
        }

        public override bool IsScanning()
        {
            return false;
        }
    }
}
