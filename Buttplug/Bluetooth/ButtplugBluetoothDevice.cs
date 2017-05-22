using System;
using Buttplug.Core;
using JetBrains.Annotations;

namespace Buttplug.Bluetooth
{
    internal class ButtplugBluetoothDevice : ButtplugDevice
    {
        [NotNull]
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
