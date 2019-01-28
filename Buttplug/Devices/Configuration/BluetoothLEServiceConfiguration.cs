using System;
using System.Collections.Generic;

namespace Buttplug.Devices.Configuration
{
    public class BluetoothLEServiceConfiguration
    {
        public readonly Guid Uuid;
        public readonly Dictionary<string, Guid> Characteristics;

        public BluetoothLEServiceConfiguration(Guid aUuid, Dictionary<string, Guid> aCharacteristics)
        {
            Uuid = aUuid;
            Characteristics = aCharacteristics;
        }
    }
}
