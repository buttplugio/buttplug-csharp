using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Buttplug.Logging;
using Buttplug.Messages;

namespace Buttplug.Core
{
    internal class DeviceManager
    {
        private readonly List<DeviceSubtypeManager> _managers;
        internal Dictionary<uint, ButtplugDevice> _devices { get; }
        private uint _deviceIndex;
        private readonly ButtplugLog _bpLogger;
        private readonly ButtplugLogManager _bpLogManager;
        public event EventHandler<MessageReceivedEventArgs> DeviceMessageReceived;

        public DeviceManager(ButtplugLogManager aLogManager)
        {
            _bpLogManager = aLogManager;
            _bpLogger = _bpLogManager.GetLogger(LogProvider.GetCurrentClassLogger());
            _bpLogger.Trace("Setting up DeviceManager");

            _devices = new Dictionary<uint, ButtplugDevice>();
            _deviceIndex = 0;

            //TODO Introspect managers based on project contents and OS version (#15)
            _managers = new List<DeviceSubtypeManager>();
            try
            {
                _managers.Add(new BluetoothManager(_bpLogManager));
            }
            catch (ReflectionTypeLoadException)
            {
                _bpLogger.Warn("Cannot bring up UWP Bluetooth manager!");
            }
            _managers.Add(new XInputGamepadManager(_bpLogManager));
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
            e.Device.DeviceRemoved += DeviceRemovedHandler;
            var msg = new DeviceAdded(_deviceIndex, e.Device.Name, e.Device.GetAllowedMessageTypesAsStrings().ToArray());
            _deviceIndex += 1;
            DeviceMessageReceived?.Invoke(this, new MessageReceivedEventArgs(msg));
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
                DeviceMessageReceived?.Invoke(this, new MessageReceivedEventArgs(new DeviceRemoved(pair.Key)));
            }
        }

        public async Task<ButtplugMessage> SendMessage(ButtplugMessage aMsg)
        {
            var id = aMsg.Id;
            switch (aMsg)
            {
                case StartScanning _:
                    StartScanning();
                    return new Ok(id);

                case StopScanning _:
                    StopScanning();
                    return new Ok(id);

                case RequestDeviceList _:
                    var msgDevices = _devices
                        .Select(d => new DeviceMessageInfo(d.Key, d.Value.Name,
                            d.Value.GetAllowedMessageTypesAsStrings().ToArray())).ToList();
                    return new DeviceList(msgDevices.ToArray(), id);

                // If it's a device message, it's most likely not ours.
                case ButtplugDeviceMessage m:
                    _bpLogger.Trace($"Sending {aMsg.GetType().Name} to device index {m.DeviceIndex}");
                    if (_devices.ContainsKey(m.DeviceIndex))
                    {
                        return await _devices[m.DeviceIndex].ParseMessage(m);
                    }
                    return ButtplugUtils.LogErrorMsg(id, _bpLogger,
                        $"Dropping message for unknown device index {m.DeviceIndex}");
            }
            return ButtplugUtils.LogErrorMsg(id, _bpLogger, $"Message type {aMsg.GetType().Name} unhandled by this server.");
        }

        private void StartScanning()
        {
            _managers.ForEach(m => m.StartScanning());
        }

        private void StopScanning()
        {
            _managers.ForEach(m => m.StopScanning());
        }

        internal void AddManager(object m)
        {
            if ((m as DeviceSubtypeManager) is null)
            {
                return;
            }
            DeviceSubtypeManager mgr = (DeviceSubtypeManager) m;
            _managers.Add(mgr);
            mgr.DeviceAdded += DeviceAddedHandler;
        }
    }
}
