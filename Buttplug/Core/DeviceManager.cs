﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Buttplug.Messages;

namespace Buttplug.Core
{
    internal class DeviceManager
    {
        private readonly List<IDeviceSubtypeManager> _managers;
        internal Dictionary<uint, IButtplugDevice> _devices { get; }
        private uint _deviceIndex;
        private readonly IButtplugLog _bpLogger;
        private readonly IButtplugLogManager _bpLogManager;
        public event EventHandler<MessageReceivedEventArgs> DeviceMessageReceived;

        public DeviceManager(IButtplugLogManager aLogManager)
        {
            _bpLogManager = aLogManager;
            _bpLogger = _bpLogManager.GetLogger(GetType());
            _bpLogger.Trace("Setting up DeviceManager");

            _devices = new Dictionary<uint, IButtplugDevice>();
            _deviceIndex = 0;

            _managers = new List<IDeviceSubtypeManager>();
        }
        
        protected IEnumerable<string> GetAllowedMessageTypesAsStrings(IButtplugDevice aDevice)
        {
            return from x in aDevice.GetAllowedMessageTypes() select x.Name;
        }

        private void DeviceAddedHandler(object o, DeviceAddedEventArgs e)
        {
            var duplicates = from x in _devices.Values
                where x.Identifier == e.Device.Identifier
                select x;
            if (duplicates.Any())
            {
                _bpLogger.Trace($"Already have device {e.Device.Name} in Devices list");
                return;
            }
            _bpLogger.Debug($"Adding Device {e.Device.Name} at index {_deviceIndex}");
            _devices.Add(_deviceIndex, e.Device);
            e.Device.DeviceRemoved += DeviceRemovedHandler;
            var msg = new DeviceAdded(_deviceIndex, e.Device.Name, GetAllowedMessageTypesAsStrings(e.Device).ToArray());
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
            var entry = (from x in _devices where x.Value.Identifier == device.Identifier select x).ToList();
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
                pair.Value.DeviceRemoved -= DeviceRemovedHandler;
                _bpLogger.Info($"Device removed: {pair.Key} - {pair.Value.Name}");
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

                case StopAllDevices _:
                    var isOk = true;
                    var errorMsg = "";
                    foreach (var d in _devices.ToList())
                    {
                        var r = await d.Value.ParseMessage(new StopDeviceCmd(d.Key, aMsg.Id));
                        if (r is Ok)
                        {
                            continue;
                        }
                        isOk = false;
                        errorMsg += $"{(r as Error).ErrorMessage}; ";
                    }
                    if (isOk)
                    {
                        return new Ok(aMsg.Id);
                    }
                    return new Error(errorMsg, aMsg.Id);
                case RequestDeviceList _:
                    var msgDevices = _devices
                        .Select(d => new DeviceMessageInfo(d.Key, d.Value.Name,
                            GetAllowedMessageTypesAsStrings(d.Value).ToArray())).ToList();
                    return new DeviceList(msgDevices.ToArray(), id);

                // If it's a device message, it's most likely not ours.
                case ButtplugDeviceMessage m:
                    _bpLogger.Trace($"Sending {aMsg.GetType().Name} to device index {m.DeviceIndex}");
                    if (_devices.ContainsKey(m.DeviceIndex))
                    {
                        return await _devices[m.DeviceIndex].ParseMessage(m);
                    }
                    return _bpLogger.LogErrorMsg(id, $"Dropping message for unknown device index {m.DeviceIndex}");
            }
            return _bpLogger.LogErrorMsg(id, $"Message type {aMsg.GetType().Name} unhandled by this server.");
        }

        private void StartScanning()
        {
            _managers.ForEach(m => m.StartScanning());
        }

        private void StopScanning()
        {
            _managers.ForEach(m => m.StopScanning());
        }

        public void AddDeviceSubtypeManager<T>(Func<IButtplugLogManager,T> aCreateMgrFunc) where T : IDeviceSubtypeManager
        {
            AddDeviceSubtypeManager(aCreateMgrFunc(_bpLogManager));
        }

        internal void AddDeviceSubtypeManager(IDeviceSubtypeManager mgr)
        {
            _managers.Add(mgr);
            mgr.DeviceAdded += DeviceAddedHandler;
        }
    }
}
