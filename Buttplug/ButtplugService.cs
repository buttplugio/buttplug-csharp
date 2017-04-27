using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Buttplug
{
    public class ButtplugService
    {
        private BluetoothManager mBluetooth;

        public ButtplugService()
        {
            mBluetooth = new BluetoothManager();
            mBluetooth.AddServiceFilter(Raunch.RAUNCH_SERVICE);
            mBluetooth.StartScanning();
        }
    }
}
