using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;
using JetBrains.Annotations;
using WebSocket4Net;

namespace Buttplug.Client.Connectors.WebsocketConnector
{
    public class ButtplugWebsocketConnector : ButtplugRemoteJSONConnector, IButtplugClientConnector
    {
        /// <summary>
        /// Guards re-entrancy of websocket message sending function.
        /// </summary>
        [NotNull] private readonly object _sendLock = new object();

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
        private bool _connecting => _connectedOrFailed?.Task != null && !_connectedOrFailed.Task.IsCompleted;

        public bool Connected => _ws != null && _ws.State != WebSocketState.Closed;

        public event EventHandler Disconnected;

        /// <summary>
        /// Used for dispatching events to the owning application context.
        /// </summary>
        private readonly SynchronizationContext _owningDispatcher = SynchronizationContext.Current ?? new SynchronizationContext();

        private readonly Uri _uri;

        private readonly bool _ignoreSSLErrors;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aURL">
        /// The URL for the Buttplug WebSocket Server, in the form of wss://address:port (wss:// is
        /// to ws:// as https:// is to http://)
        /// </param>
        /// <param name="aIgnoreSSLErrors">
        /// When using SSL (wss://), prevents bad certificates from causing connection failures
        /// </param>
        public ButtplugWebsocketConnector(Uri aUri, bool aIgnoreSSLErrors)
        {
            _uri = aUri;
            _ignoreSSLErrors = aIgnoreSSLErrors;
        }

        /// <summary>
        /// Creates the connection to the Buttplug Server and performs the protocol handshake.
        /// </summary>
        /// <returns>Nothing (Task used for async/await)</returns>
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Embedded acronyms")]
        public async Task Connect()
        {
            if (_ws != null && (_ws.State == WebSocketState.Connecting || _ws.State == WebSocketState.Open))
            {
                throw new InvalidOperationException("Already connected!");
            }

            _ws = new WebSocket(_uri.ToString());

            _connectedOrFailed = new TaskCompletionSource<object>();
            _disconnected = new TaskCompletionSource<object>();

            _ws.Opened += OpenedHandler;
            _ws.Closed += ClosedHandler;
            _ws.Error += ErrorHandler;

            if (_ignoreSSLErrors)
            {
                _ws.Security.AllowNameMismatchCertificate = true;
                _ws.Security.AllowUnstrustedCertificate = true;
                _ws.Security.AllowCertificateChainErrors = true;
            }

            _ws.Open();

            await _connectedOrFailed.Task;

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
            _disconnected.TrySetResult(null);
            Disconnect().Wait();
        }

        /// <summary>
        /// Websocket opened event handler.
        /// </summary>
        /// <param name="aSender">Object sending the open event, unused.</param>
        /// <param name="aEventArgs">Event parameters, unused.</param>

        private void OpenedHandler(object aSender, EventArgs aEventArgs)
        {
            _connectedOrFailed.TrySetResult(null);
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
                // noop - something went wrong closing the socket, but we're about to dispose of it anyway.
            }

            _ws = null;
            _owningDispatcher.Send(_ => Disconnected?.Invoke(this, new EventArgs()), null);
        }

        public async Task<ButtplugMessage> Send(ButtplugMessage aMsg)
        {
            var (msgString, msgPromise) = PrepareMessage(aMsg);
            try
            {
                lock (_sendLock)
                {
                    if (_ws != null && _ws.State == WebSocketState.Open)
                    {
                        _ws.Send(msgString);
                    }
                    else
                    {
                        return new Error("Bad WS state!", Error.ErrorClass.ERROR_UNKNOWN, ButtplugConsts.SystemMsgId);
                    }
                }

                return await msgPromise;
            }
            catch (Exception e)
            {
                // Noop - WS probably closed on us during read
                return new Error(e.Message, Error.ErrorClass.ERROR_UNKNOWN, ButtplugConsts.SystemMsgId);
            }
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
            _owningDispatcher.Send(_ => ReceiveMessages(aArgs.Message), null);
        }
    }
}