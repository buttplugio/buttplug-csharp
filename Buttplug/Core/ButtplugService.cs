using System;
using System.Collections.Generic;
using System.Linq;
using Buttplug.Messages;
using System.Threading.Tasks;

namespace Buttplug
{
    public class DeviceAddedEventArgs : EventArgs
    {
        public IButtplugDevice Device { get; }
        public DeviceAddedEventArgs(IButtplugDevice d)
        {
            this.Device = d;
        }
    }

    public class MessageReceivedEventArgs : EventArgs
    {
        public IButtplugMessage Message { get; }
        public MessageReceivedEventArgs(IButtplugMessage aMsg)
        {
            Message = aMsg;
        }
    }

    abstract class IDeviceManager
    {
        public event EventHandler<DeviceAddedEventArgs> DeviceAdded;
        protected void InvokeDeviceAdded(DeviceAddedEventArgs args)
        {
            //Can't invoke this from child classes? Weird.
            DeviceAdded?.Invoke(this, args);
        }
        abstract public void StartScanning();
        abstract public void StopScanning();
    }

    public class ButtplugService
    {
        List<IDeviceManager> Managers;
        Dictionary<uint, IButtplugDevice> Devices;
        uint DeviceIndex;
        public event EventHandler<DeviceAddedEventArgs> DeviceAdded;
        // TODO Should I just make StartScanning async across device managers?
        public event EventHandler FinishedScanning;
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public ButtplugService()
        {
            Devices = new Dictionary<uint, IButtplugDevice>();
            DeviceIndex = 0;
            Managers = new List<IDeviceManager>();
            Managers.Add(new BluetoothManager());
            Managers.Add(new XInputGamepadManager());
            Managers.ForEach(m => m.DeviceAdded += DeviceAddedHandler);
        }

        public void DeviceAddedHandler(object o, DeviceAddedEventArgs e)
        {
            Devices.Add(DeviceIndex, e.Device);
            DeviceAdded?.Invoke(this, e);
        }

        public bool SendMessage(IButtplugMessage aMsg)
        {
            switch (aMsg)
            {
                case IButtplugDeviceMessage m:
                    if (!Devices.ContainsKey(m.DeviceIndex))
                    {
                        return false;
                    }
                    return Devices[m.DeviceIndex].ParseMessage(m);
            }
            return false;
        }

        public void StartScanning()
        {
            Managers.ForEach(m => m.StartScanning());
        }

        public void StopScanning()
        {
            Managers.ForEach(m => m.StopScanning());
        }
    }
}
