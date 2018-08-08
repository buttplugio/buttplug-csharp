using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Buttplug.Core;
using Buttplug.Core.Messages;
using Buttplug.Server;
using JetBrains.Annotations;
using vtortola.WebSockets;

namespace Buttplug.Components.WebsocketServer
{
    public class ButtplugWebsocketServerSession
    {
        private readonly ButtplugServer _server;

        private readonly WebSocket _ws;

        private readonly CancellationTokenSource _internalCancelSource;

        private readonly CancellationTokenSource _linkedCancelSource;

        private IButtplugLog _logger;

        [NotNull] private BufferBlock<string> _outgoingMessages = new BufferBlock<string>();

        public ButtplugWebsocketServerSession(IButtplugLogManager aLogManager, ButtplugServer aServer, WebSocket aSocket, CancellationTokenSource aExternalCancelSource)
        {
            _logger = aLogManager.GetLogger(GetType());
            _internalCancelSource = new CancellationTokenSource();

            _linkedCancelSource = CancellationTokenSource.CreateLinkedTokenSource(aExternalCancelSource.Token, _internalCancelSource.Token);
            _ws = aSocket;
            _server = aServer;
            _server.MessageReceived += ReceiveMessageFromServerHandler;
            _server.ClientConnected += ClientConnectedHandler;
            _server.PingTimeout += PingTimeoutHandler;
        }

        public void ClientConnectedHandler(object aObject, MessageReceivedEventArgs aEvent)
        {
            var msg = aEvent.Message as RequestServerInfo;
            var clientName = msg?.ClientName ?? "Unknown client";

            //ConnectionUpdated?.Invoke(this, new ConnectionEventArgs(remoteId, clientName));
        }

        private void PingTimeoutHandler(object aObject, EventArgs aEvent)
        {
            _linkedCancelSource.Cancel();
        }

        private async Task QueueMessage(ButtplugMessage[] aMsg)
        {
            var msgStr = _server.Serialize(aMsg);
            if (msgStr == null)
            {
                return;
            }

            await _outgoingMessages.SendAsync(msgStr);
        }

        private async void ReceiveMessageFromServerHandler(object aObject, MessageReceivedEventArgs aEvent)
        {
            await QueueMessage(new[] { aEvent.Message });
        }

        public async Task RunServerSession()
        {
            try
            {
                var readTask = _ws.ReadStringAsync(_linkedCancelSource.Token);
                var writeTask = _outgoingMessages.OutputAvailableAsync(_linkedCancelSource.Token);
                while (_ws.IsConnected && !_linkedCancelSource.IsCancellationRequested)
                {
                    var msgTasks = new Task[]
                    {
                        readTask,
                        writeTask,
                    };

                    var completedTaskIndex = Task.WaitAny(msgTasks);

                    if (completedTaskIndex == 0)
                    {
                        var incomingMsg = ((Task<string>)msgTasks[0]).GetAwaiter().GetResult();
                        if (incomingMsg != null)
                        {
                            await QueueMessage(await _server.SendMessage(incomingMsg));
                        }

                        readTask = _ws.ReadStringAsync(_linkedCancelSource.Token);
                    }
                    else
                    {
                        try
                        {
                            IList<string> msgs = new List<string>();
                            _outgoingMessages.TryReceiveAll(out msgs);
                            string outmsgs = msgs.Aggregate(string.Empty, (current, msg) => current + msg);
                            if (_ws != null && _ws.IsConnected)
                            {
                                await _ws.WriteStringAsync(outmsgs, _linkedCancelSource.Token);
                            }

                            writeTask = _outgoingMessages.OutputAvailableAsync(_linkedCancelSource.Token);
                        }
                        catch (WebSocketException e)
                        {
                            // Probably means we're repling to a message we recieved just before shutdown.
                            _logger.Error(e.Message, true);
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
                await ShutdownSession();
            }
        }

        private async Task ShutdownSession()
        {
            try
            {
                await _ws.CloseAsync();
            }
            catch
            {
                // noop
            }
            _server.MessageReceived -= ReceiveMessageFromServerHandler;
            await _server.Shutdown();
            _ws.Dispose();

            //ConnectionClosed?.Invoke(this, new ConnectionEventArgs(remoteId));
        }
    }
}