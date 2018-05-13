using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;
using JetBrains.Annotations;
using WebSocket4Net;
using static Buttplug.Client.DeviceEventArgs;

namespace Buttplug.Client
{
    /// <summary>
    /// Handles communicating with a Buttplug Server over WebSockets.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class ButtplugWSClient : ButtplugAbsractClient
    {
        /// <summary>
        /// Event fired on Buttplug device added, either after connect or while scanning for devices.
        /// </summary>
        [CanBeNull]
        public override event EventHandler<DeviceEventArgs> DeviceAdded;

        /// <summary>
        /// Event fired on Buttplug device removed. Can fire at any time after device connection.
        /// </summary>
        [CanBeNull]
        public override event EventHandler<DeviceEventArgs> DeviceRemoved;

        /// <summary>
        /// Event fired when the server has finished scanning for devices.
        /// </summary>
        [CanBeNull]
        public override event EventHandler<ScanningFinishedEventArgs> ScanningFinished;

        /// <summary>
        /// Event fired when an error has been encountered. This may be internal client exceptions or
        /// Error messages from the server.
        /// </summary>
        [CanBeNull]
        public override event EventHandler<ErrorEventArgs> ErrorReceived;

        /// <summary>
        /// Event fired when the client receives a Log message. Should only fire if the client has
        /// requested that log messages be sent.
        /// </summary>
        [CanBeNull]
        public override event EventHandler<LogEventArgs> Log;

        /// <summary>
        /// Guards re-entrancy of websocket message sending function.
        /// </summary>
        [NotNull]
        private readonly object _sendLock = new object();

        /// <summary>
        /// Websocket access object.
        /// </summary>
        [CanBeNull]
        private WebSocket _ws;

        /// <summary>
        /// Signifies the end of the connection process.
        /// </summary>
        private TaskCompletionSource<bool> _connectedOrFailed;

        /// <summary>
        /// Signifies the server disconnecting from the client.
        /// </summary>
        private TaskCompletionSource<bool> _disconnected;

        /// <summary>
        /// Used for error handling during the connection process.
        /// </summary>
        private bool _connecting;

        /// <summary>
        /// Status of the client connection.
        /// </summary>
        /// <returns>True if client is currently connected.</returns>
        public override bool IsConnected => _ws != null &&
                                   (_ws.State == WebSocketState.Connecting ||
                                    _ws.State == WebSocketState.Open);

        /// <summary>
        /// Initializes a new instance of the <see cref="ButtplugWSClient"/> class.
        /// </summary>
        /// <param name="aClientName">The name of the client (used by the server for UI and permissions).</param>
        public ButtplugWSClient(string aClientName)
            : base(aClientName)
        {
        }

        /// <summary>
        /// Creates the connection to the Buttplug Server and performs the protocol handshake.
        /// </summary>
        /// <remarks>
        /// Once the WebSocket connection is open, the RequestServerInfo message is sent; the
        /// response is used to set up the ping timer loop. The RequestDeviceList message is also
        /// sent, so that any devices the server is already connected to are made known to the client.
        ///
        /// <b>Important:</b> Ensure that <see cref="DeviceAdded"/>, <see cref="DeviceRemoved"/> and
        /// <see cref="ErrorReceived"/> handlers are set before Connect is called.
        /// </remarks>
        /// <param name="aURL">
        /// The URL for the Buttplug WebSocket Server, in the form of wss://address:port (wss:// is
        /// to ws:// as https:// is to http://)
        /// </param>
        /// <param name="aIgnoreSSLErrors">
        /// When using SSL (wss://), prevents bad certificates from causing connection failures
        /// </param>
        /// <returns>Nothing (Task used for async/await)</returns>
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Embedded acronyms")]
        public override async Task Connect(Uri aURL, bool aIgnoreSSLErrors = false)
        {
            if (_ws != null && (_ws.State == WebSocketState.Connecting || _ws.State == WebSocketState.Open))
            {
                throw new InvalidOperationException("Already connected!");
            }

            _ws = new WebSocket(aURL.ToString());
            _waitingMsgs.Clear();
            _devices.Clear();
            _counter = 1;

            _connectedOrFailed = new TaskCompletionSource<bool>();
            _disconnected = new TaskCompletionSource<bool>();

            _ws.Opened += OpenedHandler;
            _ws.Closed += ClosedHandler;
            _ws.Error += ErrorHandler;

            if (aIgnoreSSLErrors)
            {
                _ws.Security.AllowNameMismatchCertificate = true;
                _ws.Security.AllowUnstrustedCertificate = true;
                _ws.Security.AllowCertificateChainErrors = true;
            }

            _ws.Open();

            _connecting = true;
            await _connectedOrFailed.Task;
            _connecting = false;

            if (_ws.State != WebSocketState.Open)
            {
                throw new Exception("Connection failed!");
            }

            _ws.MessageReceived += MessageReceivedHandler;

            var res = await SendMessage(new RequestServerInfo(_clientName));
            switch (res)
            {
                case ServerInfo si:
                    if (si.MaxPingTime > 0)
                    {
                        _pingTimer = new Timer(OnPingTimer, null, 0, Convert.ToInt32(Math.Round(((double)si.MaxPingTime) / 2, 0)));
                    }

                    if (si.MessageVersion < ButtplugMessage.CurrentSchemaVersion)
                    {
                        throw new Exception("Buttplug Server's schema version (" + si.MessageVersion +
                            ") is less than the client's (" + ButtplugMessage.CurrentSchemaVersion +
                            "). A newer server is required.");
                    }

                    // Get full device list and populate internal list
                    var resp = await SendMessage(new RequestDeviceList());
                    if ((resp as DeviceList)?.Devices == null)
                    {
                        if (resp is Error)
                        {
                            _owningDispatcher.Send(_ =>
                            {
                                ErrorReceived?.Invoke(this, new ErrorEventArgs(resp as Error));
                            }, null);
                        }

                        return;
                    }

                    foreach (var d in (resp as DeviceList).Devices)
                    {
                        if (_devices.ContainsKey(d.DeviceIndex))
                        {
                            continue;
                        }

                        var device = new ButtplugClientDevice(d);
                        if (_devices.TryAdd(d.DeviceIndex, device))
                        {
                            _owningDispatcher.Send(_ =>
                            {
                                DeviceAdded?.Invoke(this, new DeviceEventArgs(device, DeviceAction.ADDED));
                            }, null);
                        }
                    }

                    break;

                case Error e:
                    throw new Exception(e.ErrorMessage);

                default:
                    throw new Exception("Unexpecte message returned: " + res.GetType());
            }
        }

        /// <summary>
        /// Websocket error event handler.
        /// </summary>
        /// <param name="aSender">Object sending the error.</param>
        /// <param name="aEventArgs">Websocket error parameters, which contain info about the error.</param>
        private void ErrorHandler(object aSender, SuperSocket.ClientEngine.ErrorEventArgs aEventArgs)
        {
            if (_connecting)
            {
                _connectedOrFailed.TrySetResult(false);
            }

            _owningDispatcher.Send(_ =>
            {
                ErrorReceived?.Invoke(this, new ErrorEventArgs(aEventArgs.Exception));

                if (_ws?.State != WebSocketState.Open)
                {
                    Disconnect().Wait();
                }
            }, null);
        }

        /// <summary>
        /// Websocket closed event handler.
        /// </summary>
        /// <param name="aSender">Object sending the closure event, unused.</param>
        /// <param name="aEventArgs">Event parameters, unused.</param>
        private void ClosedHandler(object aSender, EventArgs aEventArgs)
        {
            _disconnected.TrySetResult(true);
            _owningDispatcher.Send(_ =>
            {
                Disconnect().Wait();
            }, null);
        }

        /// <summary>
        /// Websocket opened event handler.
        /// </summary>
        /// <param name="aSender">Object sending the open event, unused.</param>
        /// <param name="aEventArgs">Event parameters, unused.</param>
        private void OpenedHandler(object aSender, EventArgs aEventArgs)
        {
            _connectedOrFailed.TrySetResult(true);
        }

        /// <summary>
        /// Closes the WebSocket Connection.
        /// </summary>
        /// <returns>Nothing (Task used for async/await)</returns>
        public override async Task Disconnect()
        {
            if (_pingTimer != null)
            {
                _pingTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _pingTimer = null;
            }

            try
            {
                while (_ws != null && _ws.State != WebSocketState.Closed)
                {
                    if (_ws.State == WebSocketState.Closing || _ws.State == WebSocketState.Closed)
                    {
                        continue;
                    }

                    _ws.Close("Client shutdown");
                    await _disconnected.Task;
                }
            }
            catch
            {
                // noop - something when wrong closing the socket, but we're about to dispose of it anyway.
            }

            try
            {
                _tokenSource.Cancel();
            }
            catch
            {
                // noop - something when wrong closing the socket, but we're about to dispose of it anyway.
            }

            _ws = null;

            var max = 3;
            while (max-- > 0 && _waitingMsgs.Count != 0)
            {
                foreach (var msgId in _waitingMsgs.Keys)
                {
                    if (_waitingMsgs.TryRemove(msgId, out TaskCompletionSource<ButtplugMessage> promise))
                    {
                        promise.SetResult(new Error("Connection closed!", Error.ErrorClass.ERROR_UNKNOWN, ButtplugConsts.SystemMsgId));
                    }
                }
            }

            _counter = 1;
        }

        /// <summary>
        /// Websocket Message Received event handler. Either tries to match incoming messages as
        /// replies to messages we've sent, or fires an event related to an incoming event, like
        /// device additions/removals, log messages, etc.
        /// </summary>
        /// <param name="aSender">Object sending the open event, unused.</param>
        /// <param name="aArgs">Event parameters, including the data received.</param>
        private void MessageReceivedHandler(object aSender, WebSocket4Net.MessageReceivedEventArgs aArgs)
        {
            MessageReceivedHandler(Deserialize(aArgs.Message));
        }

        /// <summary>
        /// Sends a message to the server, and handles asynchronously waiting for the reply from the server.
        /// </summary>
        /// <param name="aMsg">Message to send.</param>
        /// <returns>The response, which will derive from <see cref="ButtplugMessage"/>.</returns>
        protected override async Task<ButtplugMessage> SendMessage(ButtplugMessage aMsg)
        {
            // The client always increments the IDs on outgoing messages
            aMsg.Id = NextMsgId;

            var promise = new TaskCompletionSource<ButtplugMessage>();
            _waitingMsgs.TryAdd(aMsg.Id, promise);

            var output = Serialize(aMsg);

            try
            {
                lock (_sendLock)
                {
                    if (_ws != null && _ws.State == WebSocketState.Open)
                    {
                        _ws.Send(output);
                    }
                    else
                    {
                        return new Error("Bad WS state!", Error.ErrorClass.ERROR_UNKNOWN, ButtplugConsts.SystemMsgId);
                    }
                }

                return await promise.Task;
            }
            catch (Exception e)
            {
                // Noop - WS probably closed on us during read
                return new Error(e.Message, Error.ErrorClass.ERROR_UNKNOWN, ButtplugConsts.SystemMsgId);
            }
        }
    }
}