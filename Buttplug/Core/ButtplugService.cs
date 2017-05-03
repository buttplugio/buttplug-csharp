using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NLog;
using Buttplug.Messages;

namespace Buttplug
{
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
        Dictionary<uint, ButtplugDevice> Devices;
        uint DeviceIndex;
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        Logger BPLogger;

        public ButtplugService()
        {
            BPLogger = LogManager.GetLogger("Buttplug");
            Parser = new ButtplugJSONMessageParser();
            Devices = new Dictionary<uint, ButtplugDevice>();
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
            var msg = new DeviceAddedMessage(DeviceIndex, e.Device.Name);
            DeviceIndex += 1;
            MessageReceived?.Invoke(this, new MessageReceivedEventArgs(msg));
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
