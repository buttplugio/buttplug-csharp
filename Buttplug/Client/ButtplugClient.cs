// <copyright file="ButtplugClient.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;

using Buttplug.Core.Messages;

namespace Buttplug.Client
{
    public class ButtplugClient
    {
        /// <summary>
        /// Name of the client, used for server UI/permissions.
        /// </summary>
        public readonly string Name;

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
        /// Event fired when a server ping timeout has occured.
        /// </summary>
        public event EventHandler PingTimeout;

        /// <summary>
        /// Event fired when a server disconnect has occured.
        /// </summary>
        public event EventHandler ServerDisconnect;

        /// <summary>
        /// Gets list of devices currently connected to the server.
        /// </summary>
        /// <value>
        /// A list of connected Buttplug devices.
        /// </value>
        public ButtplugClientDevice[] Devices => this._devices.Values.ToArray();

        /// <summary>
        /// Gets a value indicating whether the client is connected to a server.
        /// </summary>
        /// <value>
        /// Value indicating whether the client is connected to a server.
        /// </value>
        public bool Connected => this._connector != null && this._connector.Connected;

        /// <summary>
        /// Ping timer.
        /// </summary>
        /// <remarks>
        /// Sends a ping message to the server whenever the timer triggers. Usually runs at
        /// (requested ping interval / 2).
        /// </remarks>
        protected Timer _pingTimer;

        /// <summary>
        /// Stores information about devices currently connected to the server.
        /// </summary>
        private readonly Dictionary<uint, ButtplugClientDevice> _devices =
            new Dictionary<uint, ButtplugClientDevice>();

        /// <summary>
        /// Connector to use for the client. Can be local (server embedded), IPC, Websocket, etc...
        /// </summary>
        private readonly IButtplugClientConnector _connector;

        /// <summary>
        /// Initializes a new instance of the <see cref="ButtplugClient"/> class.
        /// </summary>
        /// <param name="aClientName">The name of the client (used by the server for UI and permissions).</param>
        /// <param name="aConnector">Connector for the client.</param>
        public ButtplugClient(string aClientName, IButtplugClientConnector aConnector)
        {
            ButtplugUtils.ArgumentNotNull(aConnector, nameof(aConnector));
            this.Name = aClientName;
            this._connector = aConnector;
            this._connector.Disconnected += (aObj, aEventArgs) =>
            {
                this.ServerDisconnect?.Invoke(aObj, aEventArgs);
            };
            this._connector.InvalidMessageReceived += this.ConnectorErrorHandler;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="ButtplugClient"/> class, closing the connector if
        /// it is still open.
        /// </summary>
        ~ButtplugClient()
        {
            this.DisconnectAsync().Wait();
        }

        // ReSharper disable once UnusedMember.Global
        public async Task ConnectAsync(CancellationToken aToken = default(CancellationToken))
        {
            if (this.Connected)
            {
                throw new ButtplugHandshakeException("Client already connected to a server.");
            }

            this._connector.MessageReceived += this.MessageReceivedHandler;
            await this._connector.ConnectAsync(aToken).ConfigureAwait(false);

            var res = await this.SendMessageAsync(new RequestServerInfo(this.Name), aToken).ConfigureAwait(false);
            switch (res)
            {
                case ServerInfo si:
                    if (si.MaxPingTime > 0)
                    {
                        this._pingTimer = new Timer(this.OnPingTimer, null, 0,
                            Convert.ToInt32(Math.Round(((double)si.MaxPingTime) / 2, 0)));
                    }

                    if (si.MessageVersion < ButtplugConsts.CurrentSpecVersion)
                    {
                        await this.DisconnectAsync().ConfigureAwait(false);
                        throw new ButtplugHandshakeException(
                            $"Buttplug Server's schema version ({si.MessageVersion}) is less than the client's ({ButtplugConsts.CurrentSpecVersion}). A newer server is required.",
                            res.Id);
                    }

                    // Get full device list and populate internal list
                    var resp = await this.SendMessageAsync(new RequestDeviceList()).ConfigureAwait(false);
                    if (!(resp is DeviceList))
                    {
                        await this.DisconnectAsync().ConfigureAwait(false);
                        if (resp is Error errResp)
                        {
                            throw ButtplugException.FromError(errResp);
                        }

                        throw new ButtplugHandshakeException(
                            "Received unknown response to DeviceList handshake query");
                    }

                    foreach (var d in (resp as DeviceList).Devices)
                    {
                        if (this._devices.ContainsKey(d.DeviceIndex))
                        {
                            continue;
                        }

                        var device = new ButtplugClientDevice(this, this.SendDeviceMessageAsync, d);
                        this._devices[d.DeviceIndex] = device;
                        this.DeviceAdded?.Invoke(this, new DeviceAddedEventArgs(device));
                    }

                    break;

                case Error e:
                    await this.DisconnectAsync().ConfigureAwait(false);
                    throw ButtplugException.FromError(e);

                default:
                    await this.DisconnectAsync().ConfigureAwait(false);
                    throw new ButtplugHandshakeException($"Unrecognized message {res.Name} during handshake", res.Id);
            }
        }

        public async Task DisconnectAsync()
        {
            if (!this.Connected)
            {
                return;
            }

            this._connector.MessageReceived -= this.MessageReceivedHandler;
            await this._connector.DisconnectAsync().ConfigureAwait(false);
            this.ServerDisconnect?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Instructs the server to start scanning for devices. New devices will be raised as <see
        /// cref="DeviceAdded"/> events. When scanning completes, an <see cref="ScanningFinished"/>
        /// event will be triggered.
        /// </summary>
        /// <param name="aToken">Cancellation token, for cancelling action externally if it is not yet finished.</param>
        /// <returns>
        /// Void on success, throws <see cref="ButtplugClientException" /> otherwise.
        /// </returns>
        // ReSharper disable once UnusedMember.Global
        public async Task StartScanningAsync(CancellationToken aToken = default(CancellationToken))
        {
            await this.SendMessageExpectOk(new StartScanning(), aToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Instructs the server to stop scanning for devices. If scanning was in progress, a <see
        /// cref="ScanningFinished"/> event will be sent when the server has stopped scanning.
        /// </summary>
        /// <param name="aToken">Cancellation token, for cancelling action externally if it is not yet finished.</param>
        /// <returns>
        /// Void on success, throws <see cref="ButtplugClientException" /> otherwise.
        /// </returns>
        // ReSharper disable once UnusedMember.Global
        public async Task StopScanningAsync(CancellationToken aToken = default(CancellationToken))
        {
            await this.SendMessageExpectOk(new StopScanning(), aToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends a DeviceMessage (e.g. <see cref="VibrateCmd"/> or <see cref="LinearCmd"/>). Handles
        /// constructing some parts of the message for the user.
        /// </summary>
        /// <param name="aDevice">The device to be controlled by the message.</param>
        /// <param name="aDeviceMsg">The device message (Id and DeviceIndex will be overriden).</param>
        /// <param name="aToken">Cancellation token, for cancelling action externally if it is not yet finished.</param>
        /// <returns>
        /// Void on success, throws <see cref="ButtplugClientException" /> otherwise.
        /// </returns>
        protected async Task SendDeviceMessageAsync(ButtplugClientDevice aDevice, ButtplugDeviceMessage aDeviceMsg, CancellationToken aToken = default(CancellationToken))
        {
            await this.SendMessageExpectOk(aDeviceMsg, aToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends a message to the server, and handles asynchronously waiting for the reply from the server.
        /// </summary>
        /// <param name="aMsg">Message to send.</param>
        /// <param name="aToken">Cancellation token, for cancelling action externally if it is not yet finished.</param>
        /// <returns>The response, which will derive from <see cref="ButtplugMessage"/>.</returns>
        protected async Task<ButtplugMessage> SendMessageAsync(ButtplugMessage aMsg, CancellationToken aToken = default(CancellationToken))
        {
            if (!this.Connected)
            {
                throw new ButtplugClientConnectorException("Client not connected.");
            }

            return await this._connector.SendAsync(aMsg, aToken).ConfigureAwait(false);
        }

        private void ConnectorErrorHandler(object aSender, ButtplugExceptionEventArgs aException)
        {
            this.ErrorReceived?.Invoke(this, aException);
        }

        /// <summary>
        /// Message Received event handler. Either tries to match incoming messages as replies to
        /// messages we've sent, or fires an event related to an incoming event, like device
        /// additions/removals, log messages, etc.
        /// </summary>
        /// <param name="aSender">Object sending the open event, unused.</param>
        /// <param name="aArgs">Event parameters, including the data received.</param>
        private async void MessageReceivedHandler(object aSender, MessageReceivedEventArgs aArgs)
        {
            var msg = aArgs.Message;

            switch (msg)
            {
                case DeviceAdded d:
                    var dev = new ButtplugClientDevice(this, this.SendDeviceMessageAsync, d);
                    this._devices.Add(d.DeviceIndex, dev);
                    this.DeviceAdded?.Invoke(this, new DeviceAddedEventArgs(dev));
                    break;

                case DeviceRemoved d:
                    if (!this._devices.ContainsKey(d.DeviceIndex))
                    {
                        this.ErrorReceived?.Invoke(this,
                            new ButtplugExceptionEventArgs(
                                new ButtplugDeviceException(
                                    "Got device removed message for unknown device.",
                                    msg.Id)));
                        return;
                    }

                    var oldDev = this._devices[d.DeviceIndex];
                    this._devices.Remove(d.DeviceIndex);
                    this.DeviceRemoved?.Invoke(this, new DeviceRemovedEventArgs(oldDev));
                    break;

                case ScanningFinished _:
                    // The scanning finished event is self explanatory and doesn't require extra arguments.
                    this.ScanningFinished?.Invoke(this, new EventArgs());
                    break;

                case Error e:
                    // This will both log the error and fire it from our ErrorReceived event handler.
                    this.ErrorReceived?.Invoke(this, new ButtplugExceptionEventArgs(ButtplugException.FromError(e)));

                    if (e.ErrorCode == Error.ErrorClass.ERROR_PING)
                    {
                        this.PingTimeout?.Invoke(this, EventArgs.Empty);
                        await this.DisconnectAsync().ConfigureAwait(false);
                    }

                    break;

                default:
                    this.ErrorReceived?.Invoke(this,
                        new ButtplugExceptionEventArgs(
                            new ButtplugMessageException(
                                $"Got unhandled message: {msg}",
                                msg.Id)));
                    break;
            }
        }

        /// <summary>
        /// Manages the ping timer, sending pings at the rate faster than requested by the server. If
        /// the ping timer handler does not run, it means the event loop is blocked, and the server
        /// will stop all devices and disconnect.
        /// </summary>
        /// <param name="aState">State of the Timer.</param>
        private async void OnPingTimer(object aState)
        {
            try
            {
                await this.SendMessageExpectOk(new Ping()).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                this.ErrorReceived?.Invoke(this, new ButtplugExceptionEventArgs(new ButtplugPingException("Exception thrown during ping update", ButtplugConsts.SystemMsgId, e)));

                // If SendMessageAsync throws, we're probably already disconnected, but just make sure.
                await this.DisconnectAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Sends a message, expecting a response of message type <see cref="Ok"/>.
        /// </summary>
        /// <param name="aMsg">Message to send.</param>
        /// <param name="aToken">Cancellation token, for cancelling action externally if it is not yet finished.</param>
        /// <returns>True if successful.</returns>
        private async Task SendMessageExpectOk(ButtplugMessage aMsg, CancellationToken aToken = default(CancellationToken))
        {
            var msg = await this.SendMessageAsync(aMsg, aToken).ConfigureAwait(false);
            switch (msg)
            {
                case Ok _:
                    return;
                case Error err:
                    throw ButtplugException.FromError(err);
                default:
                    throw new ButtplugMessageException($"Message type {msg.Name} not handled by SendMessageExpectOk", msg.Id);
            }
        }
    }
}