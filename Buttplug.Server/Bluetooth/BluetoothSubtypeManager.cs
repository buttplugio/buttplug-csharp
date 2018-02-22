using System.Collections.Generic;
using Buttplug.Core;
using Buttplug.Server.Bluetooth.Devices;
using JetBrains.Annotations;

namespace Buttplug.Server.Bluetooth
{
    public abstract class BluetoothSubtypeManager : DeviceSubtypeManager
    {
        [NotNull]
        [ItemNotNull]
        protected readonly List<IBluetoothDeviceInfo> BuiltinDevices;

        protected BluetoothSubtypeManager([NotNull] IButtplugLogManager aLogManager)
            : base(aLogManager)
        {
            // This used to go through all assemblies looking for IBluetoothDeviceInfo, but that
            // ended up constantly breaking due to Reflection issues on different platforms/setups.
            // Now we just build a new info array on manager load, keeps things simple, and outside info can be added via AddInfo calls.
            BuiltinDevices = new List<IBluetoothDeviceInfo>
            {
                new FleshlightLaunchBluetoothInfo(),
                new KiirooBluetoothInfo(),
                new KiirooGen2VibeBluetoothInfo(),
                new YoucupsBluetoothInfo(),
                new LovenseRev1BluetoothInfo(),
                new LovenseRev2BluetoothInfo(),
                new LovenseRev3BluetoothInfo(),
                new LovenseRev4BluetoothInfo(),
                new LovenseRev5BluetoothInfo(),
                new LovenseRev6BluetoothInfo(),
                new LovenseRev7BluetoothInfo(),
                new MagicMotionBluetoothInfo(),
                new VibratissimoBluetoothInfo(),
                new VorzeA10CycloneInfo(),
                new WeVibeBluetoothInfo(),
            };
        }
    }
}
