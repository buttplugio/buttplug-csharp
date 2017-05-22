using System.Collections.Generic;
using Buttplug.Bluetooth;
using Buttplug.Core;

namespace ButtplugTest.Core
{
    internal class TestBluetoothSubtypeManager : BluetoothSubtypeManager
    {
        public TestBluetoothSubtypeManager(IButtplugLogManager aLogManager) : base(aLogManager)
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
    }
}
