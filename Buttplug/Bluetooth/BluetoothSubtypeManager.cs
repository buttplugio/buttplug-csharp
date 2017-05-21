using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buttplug.Bluetooth.Devices;
using Buttplug.Core;

namespace Buttplug.Bluetooth
{
    public abstract class BluetoothSubtypeManager : DeviceSubtypeManager
    {
        protected List<IBluetoothDeviceInfo> BuiltinDevices;

        protected BluetoothSubtypeManager(IButtplugLogManager aLogManager) : base(aLogManager)
        {
            // This used to go through all assemblies looking for IBluetoothDeviceInfo, but that
            // ended up constantly breaking due to Reflection issues on different platforms/setups.
            // Now we just build a new info array on manager load, keeps things simple, and outside info can be added via AddInfo calls.
            BuiltinDevices = new List<IBluetoothDeviceInfo>
            {
                new FleshlightLaunchBluetoothInfo(),
                new KiirooBluetoothInfo(),
                new LovenseBluetoothInfo()
            };
        }

    }
}
