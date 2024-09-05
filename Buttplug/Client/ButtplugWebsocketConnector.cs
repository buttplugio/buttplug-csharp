using Buttplug.Core;

using System;
using System.Net.WebSockets;
using Buttplug.Core.Messages;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Buttplug.Client.Connectors.WebsocketConnector
{
    public class ButtplugWebsocketConnector : ButtplugRemoteJSONConnector, IButtplugClientConnector
    {
        /// <summary>
        /// Websocket access object.
        /// </summary>
        private ClientWebSocket _wsClient;
        public bool Connected => _wsClient?.State == WebSocketState.Open;

        public event EventHandler Disconnected;

        /// <summary>
        /// Used for dispatching events to the owning application context.
        /// </summary>
        private readonly SynchronizationContext _owningDispatcher = SynchronizationContext.Current ?? new SynchronizationContext();

        private readonly Uri _uri;

        private Task _readTask;

        /// <summary>
        /// </summary>
        /// <param name="uri">
        /// The URL for the Buttplug WebSocket Server, in the form of wss://address:port (wss:// is
        /// to ws:// as https:// is to http://)
        /// </param>
        /// <param name="ignoreSSLErrors">
        /// When using SSL (wss://), prevents bad certificates from causing connection failures
        /// </param>
        public ButtplugWebsocketConnector(Uri uri)
        {
            _uri = uri;
        }

        /// <summary>
        /// Creates the connection to the Buttplug Server and performs the protocol handshake.
        /// </summary>
        /// <returns>Nothing (Task used for async/await)</returns>
        public async Task ConnectAsync(CancellationToken token = default)
        {
            if (_wsClient != null)
            {
                throw new ButtplugHandshakeException("Websocket connector is already connected.");
            }

            try
            {
                _wsClient = new ClientWebSocket();
                await _wsClient.ConnectAsync(_uri, token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                throw new ButtplugClientConnectorException("Websocket Connection Exception! See Inner Exception", e);
            }

            _readTask = Task.Run(async () => await RunClientLoop(token).ConfigureAwait(false), token);
        }

        /// <summary>
        /// Closes the WebSocket Connection.
        /// </summary>
        /// <returns>Nothing (Task used for async/await)</returns>
        public async Task DisconnectAsync(CancellationToken token = default)
        {
            if (_wsClient != null && (_wsClient.State == WebSocketState.Connecting || _wsClient.State == WebSocketState.Open))
                await _wsClient.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None).ConfigureAwait(false);

            _wsClient?.Dispose();
            _wsClient = null;

            await _readTask.ConfigureAwait(false);
        }

        public async Task<ButtplugMessage> SendAsync(ButtplugMessage msg, CancellationToken cancellationToken)
        {
            if (_wsClient == null)
                throw new ButtplugException("Cannot send messages while disconnected", Error.ErrorClass.ERROR_MSG);

            var returnMsg = PrepareMessage(msg);
            await _wsClient.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(returnMsg.Message)), WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
            return await returnMsg.Promise.ConfigureAwait(false);
        }

        private async Task RunClientLoop(CancellationToken token)
        {
            try
            {
                var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);
                var buff = new byte[2048];
                while (Connected && !token.IsCancellationRequested)
                {
                    var incomingMsg = await _wsClient.ReceiveAsync(new ArraySegment<byte>(buff), token).ConfigureAwait(false);
                                        
                    if (incomingMsg.MessageType == WebSocketMessageType.Text)
                    {
                        var msgContent = System.Text.Encoding.Default.GetString(buff, 0, incomingMsg.Count);
                        ReceiveMessages(msgContent);
                     }
                }
            }
            catch (Exception e)
            {
                // TODO Figure out how to error here?
                Debug.WriteLine(e);
            }
            finally
            {
                if (_wsClient != null)
                {
                    // Clean up the websocket and fire the disconnection event.
                    _wsClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", token).Dispose();
                    _wsClient = null;
                }
                // If we somehow still have some live messages, throw exceptions so they aren't stuck.
                _owningDispatcher.Send(_ => Dispose(), null);
                _owningDispatcher.Send(_ => Disconnected?.Invoke(this, EventArgs.Empty), null);
            }
        }
    }
}