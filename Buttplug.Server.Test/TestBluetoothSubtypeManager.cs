using System.Collections.Generic;
using Buttplug.Server.Bluetooth;
using Buttplug.Core;

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
