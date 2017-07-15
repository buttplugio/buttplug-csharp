﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace Buttplug.Server
{
    internal class DeviceManager
    {
        private readonly List<IDeviceSubtypeManager> _managers;

        // Needs to be internal for tests.
        // ReSharper disable once MemberCanBePrivate.Global
        internal Dictionary<uint, IButtplugDevice> _devices { get; }

        private readonly IButtplugLog _bpLogger;
        private readonly IButtplugLogManager _bpLogManager;
        private long _deviceIndexCounter;
        private bool _sentFinished;

        public event EventHandler<MessageReceivedEventArgs> DeviceMessageReceived;

        public event EventHandler<EventArgs> ScanningFinished;

        public DeviceManager(IButtplugLogManager aLogManager)
        {
            _bpLogManager = aLogManager;
            _bpLogger = _bpLogManager.GetLogger(GetType());
            _bpLogger.Trace("Setting up DeviceManager");
            _sentFinished = true;
            _devices = new Dictionary<uint, IButtplugDevice>();
            _deviceIndexCounter = 0;

            _managers = new List<IDeviceSubtypeManager>();
        }

        private static IEnumerable<string> GetAllowedMessageTypesAsStrings([NotNull] IButtplugDevice aDevice)
        {
            return from x in aDevice.GetAllowedMessageTypes() select x.Name;
        }

        private void DeviceAddedHandler(object aObj, DeviceAddedEventArgs aEvent)
        {
            // If we get to 4 billion devices connected, this may be a problem.
            var deviceIndex = (uint)Interlocked.Increment(ref _deviceIndexCounter);

            // Devices can be turned off by the time they get to this point, at which point they end up null. Make sure the device isn't null.
            if (aEvent.Device == null)
            {
                return;
            }

            var duplicates = from x in _devices.Values
                where x.Identifier == aEvent.Device.Identifier
                select x;
            if (duplicates.Any())
            {
                _bpLogger.Trace($"Already have device {aEvent.Device.Name} in Devices list");
                return;
            }

            _bpLogger.Debug($"Adding Device {aEvent.Device.Name} at index {deviceIndex}");
            _devices.Add(deviceIndex, aEvent.Device);
            aEvent.Device.DeviceRemoved += DeviceRemovedHandler;
            var msg = new DeviceAdded(deviceIndex, aEvent.Device.Name, GetAllowedMessageTypesAsStrings(aEvent.Device).ToArray());

            DeviceMessageReceived?.Invoke(this, new MessageReceivedEventArgs(msg));
        }

        private void DeviceRemovedHandler(object aObj, EventArgs aEvent)
        {
            if ((aObj as ButtplugDevice) == null)
            {
                _bpLogger.Error("Got DeviceRemoved message from an object that is not a ButtplugDevice.");
                return;
            }

            var device = (ButtplugDevice)aObj;

            // The device itself will fire the remove event, so look it up in the dictionary and translate that for clients.
            var entry = (from x in _devices where x.Value.Identifier == device.Identifier select x).ToList();
            if (!entry.Any())
            {
                _bpLogger.Error("Got DeviceRemoved Event from object that is not in devices dictionary");
            }

            if (entry.Count > 1)
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

        private void ScanningFinishedHandler(object aObj, EventArgs aEvent)
        {
            if (_sentFinished)
            {
                return;
            }

            var done = true;
            _managers.ForEach(aMgr => done &= !aMgr.IsScanning());
            if (!done)
            {
                return;
            }

            _sentFinished = true;
            ScanningFinished?.Invoke(this, new EventArgs());
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
                    var errorMsg = string.Empty;
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

                    return new Error(errorMsg, Error.ErrorClass.ERROR_DEVICE, aMsg.Id);
                case RequestDeviceList _:
                    var msgDevices = _devices
                        .Select(aDevice => new DeviceMessageInfo(aDevice.Key, aDevice.Value.Name,
                                GetAllowedMessageTypesAsStrings(aDevice.Value).ToArray())).ToList();
                    return new DeviceList(msgDevices.ToArray(), id);

                // If it's a device message, it's most likely not ours.
                case ButtplugDeviceMessage m:
                    _bpLogger.Trace($"Sending {aMsg.GetType().Name} to device index {m.DeviceIndex}");
                    if (_devices.ContainsKey(m.DeviceIndex))
                    {
                        return await _devices[m.DeviceIndex].ParseMessage(m);
                    }

                    return _bpLogger.LogErrorMsg(id, Error.ErrorClass.ERROR_DEVICE, $"Dropping message for unknown device index {m.DeviceIndex}");
            }

            return _bpLogger.LogErrorMsg(id, Error.ErrorClass.ERROR_MSG, $"Message type {aMsg.GetType().Name} unhandled by this server.");
        }

        internal void RemoveAllDevices()
        {
            StopScanning();
            var removeDevices = _devices.Values;
            _devices.Clear();
            foreach (var d in removeDevices)
            {
                d.DeviceRemoved -= DeviceRemovedHandler;
                d.Disconnect();
            }
        }

        private void StartScanning()
        {
            _sentFinished = false;
            _managers.ForEach(aMgr => aMgr.StartScanning());
        }

        private void StopScanning()
        {
            _managers.ForEach(aMgr => aMgr.StopScanning());
        }

        public void AddDeviceSubtypeManager<T>([NotNull] Func<IButtplugLogManager, T> aCreateMgrFunc)
            where T : IDeviceSubtypeManager
        {
            AddDeviceSubtypeManager(aCreateMgrFunc(_bpLogManager));
        }

        internal void AddDeviceSubtypeManager(IDeviceSubtypeManager aMgr)
        {
            _managers.Add(aMgr);
            aMgr.DeviceAdded += DeviceAddedHandler;
            aMgr.ScanningFinished += ScanningFinishedHandler;
        }
    }
}
