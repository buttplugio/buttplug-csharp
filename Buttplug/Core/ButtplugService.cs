using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NLog;

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

    public class ButtplugService
    {
        ButtplugJSONMessageParser Parser;
        List<DeviceManager> Managers;
        Dictionary<uint, IButtplugDevice> Devices;
        uint DeviceIndex;
        public event EventHandler<DeviceAddedEventArgs> DeviceAdded;
        // TODO Should I just make StartScanning async across device managers?
        public event EventHandler FinishedScanning;
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        Logger BPLogger;

        public ButtplugService()
        {
            BPLogger = LogManager.GetLogger("Buttplug");
            Parser = new ButtplugJSONMessageParser();
            Devices = new Dictionary<uint, IButtplugDevice>();
            DeviceIndex = 0;

            //TODO Introspect managers based on project contents (#15)
            Managers = new List<DeviceManager>();
            Managers.Add(new BluetoothManager());
            Managers.Add(new XInputGamepadManager());
            Managers.ForEach(m => m.DeviceAdded += DeviceAddedHandler);
        }

        public void DeviceAddedHandler(object o, DeviceAddedEventArgs e)
        {
            BPLogger.Debug($"Adding Device {e.Device.Name} at index {DeviceIndex}");
            Devices.Add(DeviceIndex, e.Device);
            DeviceIndex += 1;
            DeviceAdded?.Invoke(this, e);
        }

        //TODO Figure out how SendMessage API should work (Stay async? Trigger internal event?) (Issue #16)
        public async Task<bool> SendMessage(IButtplugMessage aMsg)
        {
            BPLogger.Trace($"Got Message of type {aMsg.GetType().Name} to send.");
            switch (aMsg)
            {
                case IButtplugDeviceMessage m:
                    BPLogger.Trace($"Sending {aMsg.GetType().Name} to device index {m.DeviceIndex}");
                    if (!Devices.ContainsKey(m.DeviceIndex))
                    {
                        BPLogger.Warn($"Dropping message for unknown device index {m.DeviceIndex}");
                        return false;
                    }
                    return await Devices[m.DeviceIndex].ParseMessage(m);
            }
            return false;
        }

        public bool SendMessage(String aJSONMsg)
        {
            Parser.Deserialize(aJSONMsg).IfSome(m =>
            {
                SendMessage(m);
            });
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
