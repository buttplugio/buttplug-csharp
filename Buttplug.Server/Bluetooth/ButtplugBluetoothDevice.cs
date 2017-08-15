using System;
using Buttplug.Core;
using JetBrains.Annotations;

namespace Buttplug.Server.Bluetooth
{
    public class ButtplugBluetoothDevice : ButtplugDevice
    {
        [NotNull]
        protected readonly IBluetoothDeviceInterface Interface;

        [NotNull]
        protected readonly IBluetoothDeviceInfo Info;

        protected ButtplugBluetoothDevice([NotNull] IButtplugLogManager aLogManager,
            [NotNull] string aName,
            [NotNull] IBluetoothDeviceInterface aInterface,
            [NotNull] IBluetoothDeviceInfo aInfo)
            : base(aLogManager,
                   aName,
                   aInterface.GetAddress().ToString())
        {
            Interface = aInterface;
            Info = aInfo;
            Interface.DeviceRemoved += DeviceRemovedHandler;
        }

        public override void Disconnect()
        {
            Interface.Disconnect();
        }

        private void DeviceRemovedHandler(object aObject, EventArgs aEvent)
        {
            InvokeDeviceRemoved();
            Interface.DeviceRemoved -= DeviceRemovedHandler;
        }
    }
}
