using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using WebSocket4Net;

namespace Buttplug.Client.Connectors.WebsocketConnector
{
    public class ButtplugWebsocketConnector : IButtplugClientConnector
    {
        /// <summary>
        /// Guards re-entrancy of websocket message sending function.
        /// </summary>
        [NotNull] private readonly object _sendLock = new object();

        /// <summary>
        /// Used for cancelling out of websocket wait loops.
        /// </summary>
        [NotNull] private readonly CancellationTokenSource _tokenSource;

        /// <summary>
        /// Websocket access object.
        /// </summary>
        [CanBeNull] private WebSocket _ws;

        /// <summary>
        /// Signifies the end of the connection process.
        /// </summary>
        private TaskCompletionSource<object> _connectedOrFailed;

        /// <summary>
        /// Signifies the server disconnecting from the client.
        /// </summary>
        private TaskCompletionSource<object> _disconnected;

        /// <summary>
        /// Used for error handling during the connection process.
        /// </summary>
        private bool _connecting;

        /// <summary>
        /// Used for dispatching events to the owning application context.
        /// </summary>
        [NotNull] private readonly SynchronizationContext _owningDispatcher;

        public ButtplugWebsocketConnector(string aHost, uint aPort)
        {
            _tokenSource = new CancellationTokenSource();
        }

        [NotNull] private readonly ButtplugJSONConnector _jsonConnector = new ButtplugJSONConnector();

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
        public async Task Connect(Uri aURL, bool aIgnoreSSLErrors = false)
        {
            if (_ws != null && (_ws.State == WebSocketState.Connecting || _ws.State == WebSocketState.Open))
            {
                throw new InvalidOperationException("Already connected!");
            }

            _ws = new WebSocket(aURL.ToString());

            _connectedOrFailed = new TaskCompletionSource<object>();
            _disconnected = new TaskCompletionSource<object>();

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
                Task t;
                _connectedOrFailed.TrySetException(aEventArgs.Exception);
            }

            if (_ws?.State != WebSocketState.Open)
            {
                Disconnect().Wait();
            }
        }

        /// <summary>
        /// Websocket closed event handler.
        /// </summary>
        /// <param name="aSender">Object sending the closure event, unused.</param>
        /// <param name="aEventArgs">Event parameters, unused.</param>
        private void ClosedHandler(object aSender, EventArgs aEventArgs)
        {
            _disconnected.TrySetResult(true);
            _owningDispatcher.Send(_ => { Disconnect().Wait(); }, null);
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
        public async Task Disconnect()
        {
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
            var msgs = _jsonConnector.Deserialize(aArgs.Message);
            foreach (var msg in msgs)
            {
                // TODO Send messages to sorter
            }
        }
    }
}