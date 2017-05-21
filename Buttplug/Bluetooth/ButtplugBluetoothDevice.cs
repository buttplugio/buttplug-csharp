using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buttplug.Core;

namespace Buttplug.Bluetooth
{
    internal class ButtplugBluetoothDevice : ButtplugDevice
    {
        protected IBluetoothDeviceInterface Interface;

        protected ButtplugBluetoothDevice(IButtplugLogManager aLogManager,
            string aName,
            IBluetoothDeviceInterface aInterface) :
            base(aLogManager,
                aName,
                aInterface.GetAddress().ToString())
        {
            Interface = aInterface;
            Interface.DeviceRemoved += DeviceRemovedHandler;
        }

        private void DeviceRemovedHandler(object o, EventArgs e)
        {
            InvokeDeviceRemoved();
            Interface.DeviceRemoved -= DeviceRemovedHandler;
        }
    }
}
