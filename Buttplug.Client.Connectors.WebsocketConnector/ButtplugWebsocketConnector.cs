using Buttplug.Core;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
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
        [CanBeNull] private WebSocketClient _wsClient;

        [CanBeNull] private WebSocket _ws;

        public bool Connected => _ws != null && _ws.IsConnected;

        public event EventHandler Disconnected;

        /// <summary>
        /// Used for dispatching events to the owning application context.
        /// </summary>
        private readonly SynchronizationContext _owningDispatcher = SynchronizationContext.Current ?? new SynchronizationContext();

        private readonly Uri _uri;

        [NotNull] private readonly BufferBlock<string> _outgoingMessages = new BufferBlock<string>();

        private Task _readTask;

        /// <summary>
        /// </summary>
        /// <param name="aUri">
        /// The URL for the Buttplug WebSocket Server, in the form of wss://address:port (wss:// is
        /// to ws:// as https:// is to http://)
        /// </param>
        /// <param name="aIgnoreSSLErrors">
        /// When using SSL (wss://), prevents bad certificates from causing connection failures
        /// </param>
        public ButtplugWebsocketConnector(Uri aUri)
        {
            _uri = aUri;
        }

        /// <summary>
        /// Creates the connection to the Buttplug Server and performs the protocol handshake.
        /// </summary>
        /// <returns>Nothing (Task used for async/await)</returns>
        public async Task ConnectAsync(CancellationToken aToken = default(CancellationToken))
        {
            if (_ws != null)
            {
                throw new ButtplugHandshakeException(_logger, "Websocket connector is already connected.");
            }

            const int bufferSize = 1024 * 8; // 8KiB
            const int bufferPoolSize = 100 * bufferSize; // 800KiB pool
            var options = new WebSocketListenerOptions
            {
                // set send buffer size (optional but recommended)
                SendBufferSize = bufferSize,

                // set buffer manager for buffers re-use (optional but recommended)
                BufferManager = BufferManager.CreateBufferManager(bufferPoolSize, bufferSize),
            };

            // register RFC6455 protocol implementation (required)
            options.Standards.RegisterRfc6455();
            _wsClient = new WebSocketClient(options);

            try
            {
                _ws = await _wsClient.ConnectAsync(_uri, aToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                throw new ButtplugClientConnectorException(_logger, "Websocket Connection Exception! See Inner Exception", e);
            }

            _readTask = new Task(async () => { await RunClientLoop(aToken).ConfigureAwait(false); },
                aToken,
                TaskCreationOptions.LongRunning);
            _readTask.Start();
        }

        /// <summary>
        /// Closes the WebSocket Connection.
        /// </summary>
        /// <returns>Nothing (Task used for async/await)</returns>
        public async Task DisconnectAsync(CancellationToken aToken = default(CancellationToken))
        {
            try
            {
                if (_ws != null && _ws.IsConnected)
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

        public async Task<ButtplugMessage> SendAsync(ButtplugMessage aMsg, CancellationToken aToken)
        {
            var (msgString, msgPromise) = PrepareMessage(aMsg);
            await _outgoingMessages.SendAsync(msgString, aToken).ConfigureAwait(false);
            return await msgPromise.ConfigureAwait(false);
        }

        private async Task RunClientLoop(CancellationToken aToken)
        {
            try
            {
                var readTask = _ws.ReadStringAsync(aToken);
                var writeTask = _outgoingMessages.OutputAvailableAsync(aToken);
                while (_ws.IsConnected && !aToken.IsCancellationRequested)
                {
                    var msgTasks = new Task[]
                    {
                        readTask,
                        writeTask,
                    };

                    var completedTaskIndex = Task.WaitAny(msgTasks);

                    if (completedTaskIndex == 0)
                    {
                        var incomingMsg = await ((Task<string>) msgTasks[0]).ConfigureAwait(false);
                        if (incomingMsg != null)
                        {
                            ReceiveMessages(incomingMsg);
                        }

                        readTask = _ws.ReadStringAsync(aToken);
                    }
                    else
                    {
                        try
                        {
                            IList<string> msgs = new List<string>();
                            _outgoingMessages.TryReceiveAll(out msgs);
                            var outMsgs = msgs.Aggregate(string.Empty, (current, msg) => current + msg);
                            if (_ws != null && _ws.IsConnected)
                            {
                                await _ws.WriteStringAsync(outMsgs, aToken).ConfigureAwait(false);
                            }

                            writeTask = _outgoingMessages.OutputAvailableAsync(aToken);
                        }
                        catch (WebSocketException e)
                        {
                            // Probably means we're replying to a message we received just before shutdown.
                            // TODO This is on its own task. Where does it throw to?
                            throw new ButtplugClientConnectorException(_logger, "Websocket Client Read Error", e);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error(e.Message, true);
            }
            finally
            {
                // Clean up the websocket and fire the disconnection event.
                _ws.Dispose();
                _ws = null;
                // If we somehow still have some live messages, throw exceptions so they aren't stuck.
                _owningDispatcher.Send(_ => Shutdown(), null);
                _owningDispatcher.Send(_ => Disconnected?.Invoke(this, new EventArgs()), null);
            }
        }
    }
}