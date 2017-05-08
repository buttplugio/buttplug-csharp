using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Buttplug.Messages;
using NLog;
using NLog.Config;
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
        private ButtplugMessageNLogTarget _msgTarget;
        private LoggingRule _outgoingLoggingRule;

        public ButtplugService()
        {
            _bpLogger = LogManager.GetLogger(GetType().FullName);
            _bpLogger.Trace("Setting up ButtplugService");
            _parser = new ButtplugJsonMessageParser();
            _devices = new Dictionary<uint, ButtplugDevice>();
            _deviceIndex = 0;
            _msgTarget = new ButtplugMessageNLogTarget();
            _msgTarget.LogMessageReceived += LogMessageReceivedHandler;

            // External Logger Setup
            var c = LogManager.Configuration ?? new LoggingConfiguration();
            c.AddTarget("ButtplugLogger", _msgTarget);
            _outgoingLoggingRule = new LoggingRule("*", LogLevel.Off, _msgTarget);
            c.LoggingRules.Add(_outgoingLoggingRule);
            LogManager.Configuration = c;

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
            _bpLogger.Trace("Finished setting up ButtplugService");
        }

        private void LogMessageReceivedHandler(object o, ButtplugMessageNLogTarget.NLogMessageEventArgs e)
        {
            MessageReceived?.Invoke(this, new MessageReceivedEventArgs(e.LogMessage));
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

        //TODO Figure out how SendMessage API should work (Stay async? Trigger internal event?) (Issue #16)
        public async Task<Either<Error, IButtplugMessage>> SendMessage(IButtplugMessage aMsg)
        {
            _bpLogger.Trace($"Got Message of type {aMsg.GetType().Name} to send.");
            string errStr;
            if (aMsg is IButtplugMessageOutgoingOnly)
            {
                errStr = $"Message of type {aMsg.GetType().Name} cannot be sent to server!";
                _bpLogger.Warn(errStr);
                return new Error(errStr);
            }
            switch (aMsg)
            {
                case RequestLog m:
                    var c = LogManager.Configuration;
                    c.LoggingRules.Remove(_outgoingLoggingRule);
                    _outgoingLoggingRule = new LoggingRule("*", m.LogLevelObj, _msgTarget);
                    c.LoggingRules.Add(_outgoingLoggingRule);
                    LogManager.Configuration = c;
                    return new Ok();
                case StartScanning _:
                    StartScanning();
                    return new Ok();
                case StopScanning _:
                    StopScanning();
                    return new Ok();
                case RequestServerInfo _:
                    return new ServerInfo();
                case RequestDeviceList _:
                    var msgDevices = _devices.Select(d => new DeviceMessageInfo(d.Key, d.Value.Name)).ToList();
                    return new DeviceList(msgDevices.ToArray());
                // If it's a device message, it's most likely not ours.
                case IButtplugDeviceMessage m:
                    _bpLogger.Trace($"Sending {aMsg.GetType().Name} to device index {m.DeviceIndex}");
                    if (_devices.ContainsKey(m.DeviceIndex))
                    {
                        return await _devices[m.DeviceIndex].ParseMessage(m);
                    }
                    errStr = $"Dropping message for unknown device index {m.DeviceIndex}";
                    _bpLogger.Warn(errStr);
                    return new Error(errStr);
            }
            errStr = $"Dropping unhandled message type {aMsg.GetType().Name}";
            _bpLogger.Warn($"Dropping unhandled message type {aMsg.GetType().Name}");
            return new Error(errStr);
        }

        public async Task<Either<Error, IButtplugMessage>> SendMessage(string aJsonMsg)
        {
             var msg = _parser.Deserialize(aJsonMsg);
             return await msg.MatchAsync(
                 async x => await SendMessage(x),
                 x => ButtplugUtils.LogAndError(_bpLogger, LogLevel.Error, $"Cannot deserialize json message: {x}"));

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
