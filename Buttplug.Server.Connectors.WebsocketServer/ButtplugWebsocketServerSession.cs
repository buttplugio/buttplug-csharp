// <copyright file="ButtplugWebsocketServerSession.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Buttplug.Core;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using JetBrains.Annotations;
using vtortola.WebSockets;

namespace Buttplug.Server.Connectors.WebsocketServer
{
    public class ButtplugWebsocketServerSession
    {
        private readonly ButtplugServer _server;

        private readonly WebSocket _ws;

        private readonly CancellationTokenSource _internalCancelSource;

        private readonly CancellationTokenSource _linkedCancelSource;

        [CanBeNull]
        public EventHandler<ConnectionEventArgs> ConnectionAccepted;

        [CanBeNull]
        public EventHandler<ConnectionEventArgs> ConnectionClosed;

        private readonly IButtplugLog _logger;

        [NotNull]
        private BufferBlock<string> _outgoingMessages = new BufferBlock<string>();

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

        public void ClientConnectedHandler(object aObject, EventArgs aUnused)
        {
            ConnectionAccepted?.Invoke(this, new ConnectionEventArgs(_ws.RemoteEndpoint.ToString(), _server.ClientName));
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

            await _outgoingMessages.SendAsync(msgStr).ConfigureAwait(false);
        }

        private async void ReceiveMessageFromServerHandler(object aObject, MessageReceivedEventArgs aEvent)
        {
            await QueueMessage(new[] { aEvent.Message }).ConfigureAwait(false);
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
                        var incomingMsg = await ((Task<string>) msgTasks[0]).ConfigureAwait(false);
                        if (incomingMsg != null)
                        {
                            ButtplugMessage[] msg;
                            try
                            {
                                msg = await _server.SendMessageAsync(incomingMsg).ConfigureAwait(false);
                            }
                            catch (ButtplugException ex)
                            {
                                msg = new ButtplugMessage[] { ex.ButtplugErrorMessage };
                            }
                            await QueueMessage(msg).ConfigureAwait(false);
                        }

                        readTask = _ws.ReadStringAsync(_linkedCancelSource.Token);
                    }
                    else
                    {
                        try
                        {
                            _outgoingMessages.TryReceiveAll(out var msgs);
                            var outMsgs = msgs.Aggregate(string.Empty, (current, msg) => current + msg);
                            if (_ws != null && _ws.IsConnected)
                            {
                                await _ws.WriteStringAsync(outMsgs, _linkedCancelSource.Token).ConfigureAwait(false);
                            }

                            writeTask = _outgoingMessages.OutputAvailableAsync(_linkedCancelSource.Token);
                        }
                        catch (WebSocketException e)
                        {
                            // Probably means we're replying to a message we received just before shutdown.
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
                await ShutdownSession().ConfigureAwait(false);
            }
        }

        private async Task ShutdownSession()
        {
            var remoteId = _ws.RemoteEndpoint.ToString();
            try
            {
                await _ws.CloseAsync().ConfigureAwait(false);
            }
            catch
            {
                // noop
            }

            _server.MessageReceived -= ReceiveMessageFromServerHandler;
            await _server.ShutdownAsync().ConfigureAwait(false);
            _ws.Dispose();

            ConnectionClosed?.Invoke(this, new ConnectionEventArgs(remoteId));
        }
    }
}