using Buttplug.Core;

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using Buttplug.Core.Messages;
using vtortola.WebSockets;
using vtortola.WebSockets.Rfc6455;

namespace Buttplug.Client.Connectors.WebsocketConnector
{
    public class ButtplugWebsocketConnector : ButtplugRemoteJSONConnector, IButtplugClientConnector
    {
        /// <summary>
        /// Websocket access object.
        /// </summary>
        private WebSocketClient _wsClient;

        private WebSocket _ws;
        public bool Connected => _ws?.IsConnected == true;

        public event EventHandler Disconnected;

        /// <summary>
        /// Used for dispatching events to the owning application context.
        /// </summary>
        private readonly SynchronizationContext _owningDispatcher = SynchronizationContext.Current ?? new SynchronizationContext();

        private readonly Uri _uri;

        private Channel<string> _channel = Channel.CreateBounded<string>(256);

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
            if (_ws != null)
            {
                throw new ButtplugHandshakeException("Websocket connector is already connected.");
            }

            const int bufferSize = 1024 * 8; // 8KiB
            const int bufferPoolSize = 100 * bufferSize; // 800KiB pool
            var options = new WebSocketListenerOptions
            {
                // set send buffer size (optional but recommended)
                SendBufferSize = bufferSize,

                // set buffer manager for buffers re-use (optional but recommended)
                BufferManager = BufferManager.CreateBufferManager(bufferPoolSize, bufferSize),

                // Turn off client side ping, server will manage this.
                PingTimeout = Timeout.InfiniteTimeSpan,
                PingMode = PingMode.Manual,
            };

            // register RFC6455 protocol implementation (required)
            options.Standards.RegisterRfc6455();
            _wsClient = new WebSocketClient(options);

            try
            {
                _ws = await _wsClient.ConnectAsync(_uri, token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                throw new ButtplugClientConnectorException("Websocket Connection Exception! See Inner Exception", e);
            }

            _readTask = new Task(async () => await RunClientLoop(token).ConfigureAwait(false),
                token,
                TaskCreationOptions.LongRunning);
            _readTask.Start();
        }

        /// <summary>
        /// Closes the WebSocket Connection.
        /// </summary>
        /// <returns>Nothing (Task used for async/await)</returns>
        public async Task DisconnectAsync(CancellationToken token = default)
        {
            try
            {
                if (_ws?.IsConnected == true)
                {
                    await _ws.CloseAsync().ConfigureAwait(false);
                }
            }
            catch
            {
                // noop - something went wrong closing the socket, but we're about to dispose of it anyway.
            }

            // If we have a live task, wait for it to shut down and fire the disconnection event
            await _readTask.ConfigureAwait(false);
        }

        public async Task<ButtplugMessage> SendAsync(ButtplugMessage msg, CancellationToken token)
        {
            var (msgString, msgPromise) = PrepareMessage(msg);
            await _channel.Writer.WriteAsync(msgString);
            return await msgPromise.ConfigureAwait(false);
        }

        private async Task RunClientLoop(CancellationToken token)
        {
            try
            {
                var readTask = _ws.ReadStringAsync(token);
                var writeTask = _channel.Reader.ReadAsync(token).AsTask();
                while (_ws.IsConnected && !token.IsCancellationRequested)
                {
                    var msgTasks = new Task[]
                    {
                        readTask,
                        writeTask,
                    };

                    var completedTaskIndex = Task.WaitAny(msgTasks);

                    if (completedTaskIndex == 0)
                    {
                        var incomingMsg = await ((Task<string>)msgTasks[0]).ConfigureAwait(false);
                        if (incomingMsg != null)
                        {
                            ReceiveMessages(incomingMsg);
                        }

                        readTask = _ws.ReadStringAsync(token);
                    }
                    else
                    {
                        try
                        {
                            var outMsgs = await ((Task<string>)msgTasks[1]).ConfigureAwait(false);
                            if (_ws?.IsConnected == true)
                            {
                                await _ws.WriteStringAsync(outMsgs, token).ConfigureAwait(false);
                            }

                            writeTask = _channel.Reader.ReadAsync(token).AsTask();
                        }
                        catch (WebSocketException e)
                        {
                            // Probably means we're replying to a message we received just before shutdown.
                            // TODO This is on its own task. Where does it throw to?
                            throw new ButtplugClientConnectorException("Websocket Client Read Error", e);
                        }
                    }
                }
            }
            catch
            {
                // TODO Figure out how to error here?
            }
            finally
            {
                // Clean up the websocket and fire the disconnection event.
                _ws.Dispose();
                _ws = null;
                // If we somehow still have some live messages, throw exceptions so they aren't stuck.
                _owningDispatcher.Send(_ => Dispose(), null);
                _owningDispatcher.Send(_ => Disconnected?.Invoke(this, EventArgs.Empty), null);
            }
        }
    }
}