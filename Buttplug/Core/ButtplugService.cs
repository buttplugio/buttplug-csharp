using Buttplug.Messages;
using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using Buttplug.Logging;

namespace Buttplug.Core
{
    public class MessageReceivedEventArgs : EventArgs
    {
        public ButtplugMessage Message { get; }

        public MessageReceivedEventArgs(ButtplugMessage aMsg)
        {
            Message = aMsg;
        }
    }

    public class ButtplugService
    {
        private readonly ButtplugJsonMessageParser _parser;
        private readonly List<DeviceSubtypeManager> _managers;
        private Dictionary<uint, ButtplugDevice> _devices { get; }
        private uint _deviceIndex;

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        private readonly ILog _bpLogger;

        public ButtplugService()
        {
            _bpLogger = LogProvider.GetCurrentClassLogger();
            _bpLogger.Trace("Setting up ButtplugService");
            _parser = new ButtplugJsonMessageParser();
            _devices = new Dictionary<uint, ButtplugDevice>();
            _deviceIndex = 0;

            //TODO Introspect managers based on project contents and OS version (#15)
            _managers = new List<DeviceSubtypeManager>();
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
            _bpLogger.Trace("Finished setting up ButtplugService");
        }

        //private void LogMessageReceivedHandler(object o, ButtplugMessageNLogTarget.NLogMessageEventArgs e)
        //{
        //    MessageReceived?.Invoke(this, new MessageReceivedEventArgs(e.LogMessage));
        //}

        private void DeviceAddedHandler(object o, DeviceAddedEventArgs e)
        {
            if (_devices.ContainsValue(e.Device))
            {
                _bpLogger.Trace($"Already have device {e.Device.Name} in Devices list");
                return;
            }
            _bpLogger.Debug($"Adding Device {e.Device.Name} at index {_deviceIndex}");
            _devices.Add(_deviceIndex, e.Device);
            e.Device.DeviceRemoved += DeviceRemovedHandler;
            var msg = new DeviceAdded(_deviceIndex, e.Device.Name, e.Device.GetAllowedMessageTypesAsStrings().ToArray());
            _deviceIndex += 1;
            MessageReceived?.Invoke(this, new MessageReceivedEventArgs(msg));
        }

        private void DeviceRemovedHandler(object o, EventArgs e)
        {
            if ((o as ButtplugDevice) == null)
            {
                _bpLogger.Error("Got DeviceRemoved message from an object that is not a ButtplugDevice.");
                return;
            }
            var device = (ButtplugDevice) o;
            // The device itself will fire the remove event, so look it up in the dictionary and translate that for clients.
            var entry = (from x in _devices where x.Value == device select x).ToList();
            if (!entry.Any())
            {
                _bpLogger.Error("Got DeviceRemoved Event from object that is not in devices dictionary");
            }
            if (entry.Count() > 1)
            {
                _bpLogger.Error("Device being removed has multiple entries in device dictionary.");
            }
            foreach (var pair in entry.ToList())
            {
                _devices.Remove(pair.Key);
                MessageReceived?.Invoke(this, new MessageReceivedEventArgs(new DeviceRemoved(pair.Key)));
            }
        }

        public async Task<ButtplugMessage> SendMessage(ButtplugMessage aMsg)
        {
            _bpLogger.Trace($"Got Message {aMsg.Id} of type {aMsg.GetType().Name} to send");
            var id = aMsg.Id;
            if (id == 0)
            {
                return ButtplugUtils.LogWarnMsg(id, _bpLogger,
                    $"Message Id 0 is reserved for outgoing system messages. Please use another Id.");
            }
            if (aMsg is IButtplugMessageOutgoingOnly)
            {
                return ButtplugUtils.LogWarnMsg(id, _bpLogger,
                    $"Message of type {aMsg.GetType().Name} cannot be sent to server");
            }
            switch (aMsg)
            {
                case RequestLog m:
                    //var c = LogManager.Configuration;
                    //c.LoggingRules.Remove(_outgoingLoggingRule);
                    //_outgoingLoggingRule = new LoggingRule("*", m.LogLevelObj, _msgTarget);
                    //c.LoggingRules.Add(_outgoingLoggingRule);
                    //LogManager.Configuration = c;
                    return new Error("Logging Disabled!", id);

                case StartScanning _:
                    StartScanning();
                    return new Ok(id);

                case StopScanning _:
                    StopScanning();
                    return new Ok(id);

                case RequestServerInfo _:
                    return new ServerInfo(id);

                case Test m:
                    return new Test(m.TestString, id);

                case RequestDeviceList _:
                    var msgDevices = _devices.Select(d => new DeviceMessageInfo(d.Key, d.Value.Name, d.Value.GetAllowedMessageTypesAsStrings().ToArray())).ToList();
                    return new DeviceList(msgDevices.ToArray(), id);

                // If it's a device message, it's most likely not ours.
                case ButtplugDeviceMessage m:
                    _bpLogger.Trace($"Sending {aMsg.GetType().Name} to device index {m.DeviceIndex}");
                    if (_devices.ContainsKey(m.DeviceIndex))
                    {
                        return await _devices[m.DeviceIndex].ParseMessage(m);
                    }
                    return ButtplugUtils.LogWarnMsg(id, _bpLogger,
                        $"Dropping message for unknown device index {m.DeviceIndex}");
            }
            return ButtplugUtils.LogWarnMsg(id, _bpLogger,
                $"Dropping unhandled message type {aMsg.GetType().Name}");
        }

        public async Task<ButtplugMessage> SendMessage(string aJsonMsg)
        {
            var msg = _parser.Deserialize(aJsonMsg);
            return await msg.MatchAsync(
                async x => await SendMessage(x),
                x => ButtplugUtils.LogErrorMsg(ButtplugConsts.SYSTEM_MSG_ID, _bpLogger,
                        $"Cannot deserialize json message: {x}"));
        }

        private void StartScanning()
        {
            _managers.ForEach(m => m.StartScanning());
        }

        private void StopScanning()
        {
            _managers.ForEach(m => m.StopScanning());
        }

        protected void AddManager(object m)
        {
            if ((m as DeviceSubtypeManager) is null)
            {
                return;
            }
            DeviceSubtypeManager mgr = m as DeviceSubtypeManager;
            _managers.Add(m as DeviceSubtypeManager);
            mgr.DeviceAdded += DeviceAddedHandler;
        }
    }
}