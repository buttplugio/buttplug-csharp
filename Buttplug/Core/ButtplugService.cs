using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Buttplug.Messages;
using NLog;
using LanguageExt;

namespace Buttplug.Core
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
        private readonly ButtplugJsonMessageParser _parser;
        private readonly List<DeviceManager> _managers;
        private readonly Dictionary<uint, ButtplugDevice> _devices;
        private uint _deviceIndex;
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        private readonly Logger _bpLogger;

        public ButtplugService()
        {
            _bpLogger = LogManager.GetLogger("Buttplug");
            _parser = new ButtplugJsonMessageParser();
            _devices = new Dictionary<uint, ButtplugDevice>();
            _deviceIndex = 0;

            //TODO Introspect managers based on project contents and OS version (#15)
            _managers = new List<DeviceManager>();
            try
            {
                _managers.Add(new BluetoothManager());
            }
            catch (ReflectionTypeLoadException)
            {
                _bpLogger.Warn("Cannot bring up UWP Bluetooth manager!");
            }
            _managers.Add(new XInputGamepadManager());
            _managers.ForEach(m => m.DeviceAdded += DeviceAddedHandler);
        }

        private void DeviceAddedHandler(object o, DeviceAddedEventArgs e)
        {
            if (_devices.ContainsValue(e.Device))
            {
                _bpLogger.Trace($"Already have device {e.Device.Name} in Devices list");
                return;
            }
            _bpLogger.Debug($"Adding Device {e.Device.Name} at index {_deviceIndex}");
            _devices.Add(_deviceIndex, e.Device);
            var msg = new Messages.DeviceAdded(_deviceIndex, e.Device.Name);
            _deviceIndex += 1;
            MessageReceived?.Invoke(this, new MessageReceivedEventArgs(msg));
        }

        private void SendDeviceList()
        {
            var msgDevices = _devices.Select(d => new DeviceMessageInfo(d.Key, d.Value.Name)).ToList();
            MessageReceived?.Invoke(this, 
                                    new MessageReceivedEventArgs(new DeviceList(msgDevices.ToArray())));
        }

        //TODO Figure out how SendMessage API should work (Stay async? Trigger internal event?) (Issue #16)
        public async Task<bool> SendMessage(IButtplugMessage aMsg)
        {
            _bpLogger.Trace($"Got Message of type {aMsg.GetType().Name} to send.");
            var err = aMsg.Check();
            if (err.IsSome)
            {
                err.IfSome(x =>
                {
                    // TODO Send error replies based on message IDs
                    _bpLogger.Warn($"Got a malformed IButtplugMessage: {x}");
                });
                return false;
            }
            if (aMsg is IButtplugMessageOutgoingOnly)
            {
                _bpLogger.Warn($"Message of type {aMsg.GetType().Name} cannot be sent to server!");
                return false;
            }
            switch (aMsg)
            {
                case RequestDeviceList m:
                    SendDeviceList();
                    return true;
                // If it's a device message, it's most likely not ours.
                case IButtplugDeviceMessage m:
                    _bpLogger.Trace($"Sending {aMsg.GetType().Name} to device index {m.DeviceIndex}");
                    if (_devices.ContainsKey(m.DeviceIndex))
                    {
                        return await _devices[m.DeviceIndex].ParseMessage(m);
                    }
                    _bpLogger.Warn($"Dropping message for unknown device index {m.DeviceIndex}");
                    return false;
            }
            _bpLogger.Warn($"Dropping unhandled message type {aMsg.GetType().Name}");
            return false;
        }

        public async Task SendMessage(string aJsonMsg)
        {
            var msg = _parser.Deserialize(aJsonMsg);
            await msg.Match(async x => _bpLogger.Warn($"Cannot deserialize json message: {x}"),
                            async x => await SendMessage(x));
        }

        private void StartScanning()
        {
            _managers.ForEach(m => m.StartScanning());
        }

        private void StopScanning()
        {
            _managers.ForEach(m => m.StopScanning());
        }
    }
}
