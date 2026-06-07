// <copyright file="ButtplugClient.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;

namespace Buttplug.Client
{
    /// <summary>
    /// Main client class for connecting to Buttplug servers and managing devices.
    /// </summary>
    public class ButtplugClient :
#if NETSTANDARD2_1_OR_GREATER
        IDisposable, IAsyncDisposable
#else
        IDisposable
#endif
    {
        /// <summary>
        /// Name of the client, used for server UI/permissions.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Event fired on Buttplug device added, either after connect or while scanning for devices.
        /// </summary>
        public event EventHandler<DeviceAddedEventArgs> DeviceAdded;

        /// <summary>
        /// Event fired on Buttplug device removed. Can fire at any time after device connection.
        /// </summary>
        public event EventHandler<DeviceRemovedEventArgs> DeviceRemoved;

        /// <summary>
        /// Fires when an error that was not provoked by a client action is received from the server,
        /// such as a device exception, message parsing error, etc... Server may possibly disconnect
        /// after this event fires.
        /// </summary>
        public event EventHandler<ButtplugExceptionEventArgs> ErrorReceived;

        /// <summary>
        /// Event fired when the server has finished scanning for devices.
        /// </summary>
        public event EventHandler ScanningFinished;

        /// <summary>
        /// Event fired when a server ping timeout has occurred.
        /// </summary>
        public event EventHandler PingTimeout;

        /// <summary>
        /// Event fired when a server disconnect has occurred.
        /// </summary>
        public event EventHandler ServerDisconnect;

        /// <summary>
        /// Event fired when an InputReading is received (for subscribed sensors).
        /// </summary>
        public event EventHandler<InputReadingEventArgs> InputReadingReceived;

        /// <summary>
        /// Gets list of devices currently connected to the server.
        /// </summary>
        public ButtplugClientDevice[] Devices => _devices.Values.ToArray();

        /// <summary>
        /// Gets a value indicating whether the client is connected to a server.
        /// </summary>
        public bool Connected => _connector?.Connected == true;

        /// <summary>
        /// Ping timer.
        /// </summary>
        protected Timer _pingTimer;

        internal ButtplugClientMessageHandler _handler;

        /// <summary>
        /// Stores information about devices currently connected to the server.
        /// </summary>
        private readonly ConcurrentDictionary<uint, ButtplugClientDevice> _devices =
            new ConcurrentDictionary<uint, ButtplugClientDevice>();

        /// <summary>
        /// Connector to use for the client. Can be local (server embedded), IPC, Websocket, etc...
        /// </summary>
        private IButtplugClientConnector _connector;

        /// <summary>
        /// Initializes a new instance of the <see cref="ButtplugClient"/> class.
        /// </summary>
        /// <param name="clientName">The name of the client (used by the server for UI and permissions).</param>
        public ButtplugClient(string clientName)
        {
            Name = clientName;
        }

        /// <summary>
        /// Connects to a Buttplug server using the provided connector.
        /// </summary>
        /// <param name="connector">The connector to use for communication.</param>
        /// <param name="token">Cancellation token.</param>
        public async Task ConnectAsync(IButtplugClientConnector connector, CancellationToken token = default)
        {
            if (Connected)
            {
                throw new ButtplugHandshakeException("Client already connected to a server.");
            }
            ButtplugUtils.ArgumentNotNull(connector, nameof(connector));

            // Reset client internals
            _connector = connector;
            _connector.Disconnected += (obj, eventArgs) => ServerDisconnect?.Invoke(obj, eventArgs);
            _connector.InvalidMessageReceived += ConnectorErrorHandler;
            _connector.MessageReceived += MessageReceivedHandler;
            _devices.Clear();
            _handler = new ButtplugClientMessageHandler(connector);

            await _connector.ConnectAsync(token).ConfigureAwait(false);

            var res = await _handler.SendMessageAsync(new RequestServerInfo(Name), token).ConfigureAwait(false);
            switch (res)
            {
                case ServerInfo si:
                    if (si.MaxPingTime > 0)
                    {
                        _pingTimer?.Dispose();
                        _pingTimer = new Timer(OnPingTimer, null, 0,
                            Convert.ToInt32(Math.Round(si.MaxPingTime / 2.0, 0)));
                    }

                    if (si.ProtocolVersionMajor < ButtplugConsts.ProtocolVersionMajor)
                    {
                        await DisconnectAsync().ConfigureAwait(false);
                        throw new ButtplugHandshakeException(
                            $"Buttplug Server's protocol version ({si.ProtocolVersionMajor}.{si.ProtocolVersionMinor}) is less than the client's ({ButtplugConsts.ProtocolVersionMajor}.{ButtplugConsts.ProtocolVersionMinor}). A newer server is required.",
                            res.Id);
                    }

                    // Get full device list and populate internal list
                    await RequestDeviceListAsync(token).ConfigureAwait(false);
                    break;

                case Error e:
                    await DisconnectAsync().ConfigureAwait(false);
                    throw ButtplugException.FromError(e);

                default:
                    await DisconnectAsync().ConfigureAwait(false);
                    throw new ButtplugHandshakeException($"Unrecognized message {res.Name} during handshake", res.Id);
            }
        }

        /// <summary>
        /// Disconnects from the server.
        /// </summary>
        public async Task DisconnectAsync()
        {
            if (!Connected)
            {
                return;
            }

            _connector.MessageReceived -= MessageReceivedHandler;
            await _connector.DisconnectAsync().ConfigureAwait(false);
            ServerDisconnect?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Requests the device list from the server and updates internal state.
        /// This also handles device added/removed events by diffing against the previous list.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        private async Task RequestDeviceListAsync(CancellationToken token = default)
        {
            var resp = await _handler.SendMessageAsync(new RequestDeviceList(), token).ConfigureAwait(false);
            if (resp is DeviceList list)
            {
                ProcessDeviceList(list);
            }
            else if (resp is Error errResp)
            {
                throw ButtplugException.FromError(errResp);
            }
            else
            {
                throw new ButtplugMessageException("Received unknown response to DeviceList query");
            }
        }

        /// <summary>
        /// Processes a DeviceList message, adding new devices and removing ones that are no longer present.
        /// </summary>
        /// <param name="list">The device list from the server.</param>
        private void ProcessDeviceList(DeviceList list)
        {
            // Build a set of device indices from the incoming list
            var incomingDeviceIndices = new HashSet<uint>();
            foreach (var deviceInfo in list.GetAllDevices())
            {
                incomingDeviceIndices.Add(deviceInfo.DeviceIndex);

                if (!_devices.ContainsKey(deviceInfo.DeviceIndex))
                {
                    // New device - add it
                    var device = new ButtplugClientDevice(_handler, deviceInfo);
                    _devices[deviceInfo.DeviceIndex] = device;
                    DeviceAdded?.Invoke(this, new DeviceAddedEventArgs(device));
                }
            }

            // Find devices that were removed (in our list but not in incoming list)
            var removedIndices = _devices.Keys.Where(idx => !incomingDeviceIndices.Contains(idx)).ToList();
            foreach (var index in removedIndices)
            {
                if (_devices.TryRemove(index, out var removedDevice))
                {
                    DeviceRemoved?.Invoke(this, new DeviceRemovedEventArgs(removedDevice));
                }
            }
        }

        /// <summary>
        /// Instructs the server to start scanning for devices. New devices will be raised as
        /// <see cref="DeviceAdded"/> events. When scanning completes, a <see cref="ScanningFinished"/>
        /// event will be triggered.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        public async Task StartScanningAsync(CancellationToken token = default)
        {
            await _handler.SendMessageExpectOk(new StartScanning(), token).ConfigureAwait(false);
        }

        /// <summary>
        /// Instructs the server to stop scanning for devices. If scanning was in progress, a
        /// <see cref="ScanningFinished"/> event will be sent when the server has stopped scanning.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        public async Task StopScanningAsync(CancellationToken token = default)
        {
            await _handler.SendMessageExpectOk(new StopScanning(), token).ConfigureAwait(false);
        }

        /// <summary>
        /// Instructs the server to send stop command to all connected devices.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        public async Task StopAllDevicesAsync(CancellationToken token = default)
        {
            await _handler.SendMessageExpectOk(new StopCmd(), token).ConfigureAwait(false);
        }

        private void ConnectorErrorHandler(object sender, ButtplugExceptionEventArgs exception)
        {
            ErrorReceived?.Invoke(this, exception);
        }

        /// <summary>
        /// Message Received event handler. Either tries to match incoming messages as replies to
        /// messages we've sent, or fires an event related to an incoming event, like device
        /// list updates, scanning finished, input readings, etc.
        /// </summary>
        private async void MessageReceivedHandler(object sender, MessageReceivedEventArgs args)
        {
            var msg = args.Message;

            switch (msg)
            {
                case DeviceList deviceList:
                    // Server sent a device list - process additions and removals
                    ProcessDeviceList(deviceList);
                    break;

                case ScanningFinished _:
                    // Request updated device list when scanning finishes
                    try
                    {
                        await RequestDeviceListAsync().ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        ErrorReceived?.Invoke(this,
                            new ButtplugExceptionEventArgs(
                                new ButtplugMessageException($"Failed to get device list after scanning: {e.Message}")));
                    }
                    ScanningFinished?.Invoke(this, EventArgs.Empty);
                    break;

                case InputReading reading:
                    // Input reading from a subscribed sensor
                    InputReadingReceived?.Invoke(this, new InputReadingEventArgs(reading));
                    break;

                case Error e:
                    // This will both log the error and fire it from our ErrorReceived event handler.
                    ErrorReceived?.Invoke(this, new ButtplugExceptionEventArgs(ButtplugException.FromError(e)));

                    if (e.ErrorCode == Error.ErrorClass.ERROR_PING)
                    {
                        PingTimeout?.Invoke(this, EventArgs.Empty);
                        await DisconnectAsync().ConfigureAwait(false);
                    }
                    break;

                default:
                    ErrorReceived?.Invoke(this,
                        new ButtplugExceptionEventArgs(
                            new ButtplugMessageException(
                                $"Got unhandled message: {msg}",
                                msg.Id)));
                    break;
            }
        }

        /// <summary>
        /// Manages the ping timer, sending pings at a rate faster than requested by the server.
        /// If the ping timer handler does not run, it means the event loop is blocked, and the server
        /// will stop all devices and disconnect.
        /// </summary>
        private async void OnPingTimer(object state)
        {
            try
            {
                await _handler.SendMessageExpectOk(new Ping()).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                ErrorReceived?.Invoke(this, new ButtplugExceptionEventArgs(
                    new ButtplugPingException("Exception thrown during ping update", ButtplugConsts.SystemMsgId, e)));

                // If SendMessageAsync throws, we're probably already disconnected, but just make sure.
                await DisconnectAsync().ConfigureAwait(false);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            _pingTimer?.Dispose();
            DisconnectAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="ButtplugClient"/> class, closing the connector if
        /// it is still open.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

#if NETSTANDARD2_1_OR_GREATER
        public async ValueTask DisposeAsync()
        {
            _pingTimer?.Dispose();
            await DisconnectAsync().ConfigureAwait(false);
            GC.SuppressFinalize(this);
        }
#endif
    }

    /// <summary>
    /// Event arguments for device added events.
    /// </summary>
    public class DeviceAddedEventArgs : EventArgs
    {
        /// <summary>
        /// The device that was added.
        /// </summary>
        public ButtplugClientDevice Device { get; }

        public DeviceAddedEventArgs(ButtplugClientDevice device)
        {
            Device = device;
        }
    }

    /// <summary>
    /// Event arguments for device removed events.
    /// </summary>
    public class DeviceRemovedEventArgs : EventArgs
    {
        /// <summary>
        /// The device that was removed.
        /// </summary>
        public ButtplugClientDevice Device { get; }

        public DeviceRemovedEventArgs(ButtplugClientDevice device)
        {
            Device = device;
        }
    }

    /// <summary>
    /// Event arguments for input reading events.
    /// </summary>
    public class InputReadingEventArgs : EventArgs
    {
        /// <summary>
        /// The input reading received.
        /// </summary>
        public InputReading Reading { get; }

        /// <summary>
        /// The device index this reading is from.
        /// </summary>
        public uint DeviceIndex => Reading.DeviceIndex;

        /// <summary>
        /// The feature index this reading is from.
        /// </summary>
        public uint FeatureIndex => Reading.FeatureIndex;

        public InputReadingEventArgs(InputReading reading)
        {
            Reading = reading;
        }
    }
}
