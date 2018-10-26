// <copyright file="ButtplugIPCServer.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace Buttplug.Server.Connectors.IPCServer
{
    public class ButtplugIPCServer
    {
        [NotNull]
        private Func<ButtplugServer> _serverFactory;

        [NotNull]
        private IButtplugLogManager _logManager;

        [NotNull]
        private IButtplugLog _logger;

        [CanBeNull]
        public EventHandler<UnhandledExceptionEventArgs> OnException;

        [CanBeNull]
        public EventHandler<IPCConnectionEventArgs> ConnectionAccepted;

        [CanBeNull]
        public EventHandler<IPCConnectionEventArgs> ConnectionUpdated;

        [CanBeNull]
        public EventHandler<IPCConnectionEventArgs> ConnectionClosed;

        [NotNull]
        private ConcurrentQueue<NamedPipeServerStream> _connections = new ConcurrentQueue<NamedPipeServerStream>();

        [NotNull]
        private CancellationTokenSource _cancellation;

        [CanBeNull]
        private Task _acceptTask;

        public bool Connected => _acceptTask?.Status == TaskStatus.Running;

        public void StartServer([NotNull] Func<ButtplugServer> aFactory, string aPipeName = "ButtplugPipe")
        {
            _cancellation = new CancellationTokenSource();
            _serverFactory = aFactory;

            _logManager = new ButtplugLogManager();
            _logger = _logManager.GetLogger(GetType());

            _acceptTask = new Task(async () => { await ConnectionAccepter(aPipeName, _cancellation.Token).ConfigureAwait(false); }, _cancellation.Token, TaskCreationOptions.LongRunning);
            _acceptTask.Start();
        }

        private async Task ConnectionAccepter(string aPipeName, CancellationToken aToken)
        {
            while (!aToken.IsCancellationRequested)
            {
                var pipeServer = new NamedPipeServerStream(aPipeName, PipeDirection.InOut, 10, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                await pipeServer.WaitForConnectionAsync(aToken).ConfigureAwait(false);
                if (!pipeServer.IsConnected)
                {
                    continue;
                }

                var aServer = pipeServer;
                ConnectionAccepted?.Invoke(this, new IPCConnectionEventArgs());

                var buttplugServer = _serverFactory();

                void MsgReceived(object aObject, MessageReceivedEventArgs aEvent)
                {
                    var msg = buttplugServer.Serialize(aEvent.Message);
                    if (msg == null)
                    {
                        return;
                    }

                    try
                    {
                        if (aServer != null && aServer.IsConnected)
                        {
                            var output = Encoding.UTF8.GetBytes(msg);
                            aServer.WriteAsync(output, 0, output.Length, aToken);
                        }

                        if (aEvent.Message is Error && (aEvent.Message as Error).ErrorCode == Error.ErrorClass.ERROR_PING && aServer != null && aServer.IsConnected)
                        {
                            aServer.Close();
                        }
                    }
                    catch (WebSocketException e)
                    {
                        // Probably means we're replying to a message we received just before shutdown.
                        _logger.Error(e.Message, true);
                    }
                }

                buttplugServer.MessageReceived += MsgReceived;

                void ClientConnected(object aObject, EventArgs aUnused)
                {
                    ConnectionUpdated?.Invoke(this, new IPCConnectionEventArgs(buttplugServer.ClientName));
                }

                buttplugServer.ClientConnected += ClientConnected;

                try
                {
                    _connections.Enqueue(aServer);
                    while (!aToken.IsCancellationRequested && aServer.IsConnected)
                    {
                        var buffer = new byte[4096];
                        var msg = string.Empty;
                        var len = -1;
                        while (len < 0 || (len == buffer.Length && buffer[4095] != '\0'))
                        {
                            try
                            {
                                len = await aServer.ReadAsync(buffer, 0, buffer.Length, aToken).ConfigureAwait(false);
                                if (len > 0)
                                {
                                    msg += Encoding.UTF8.GetString(buffer, 0, len);
                                }
                            }
                            catch
                            {
                                // no-op?
                            }
                        }

                        if (msg.Length > 0)
                        {
                            ButtplugMessage[] respMsgs;
                            try
                            {
                                respMsgs = await buttplugServer.SendMessageAsync(msg).ConfigureAwait(false);
                            }
                            catch (ButtplugException e)
                            {
                                respMsgs = new ButtplugMessage[] { e.ButtplugErrorMessage };
                            }
                            
                            var respMsg = buttplugServer.Serialize(respMsgs);
                            if (respMsg == null)
                            {
                                continue;
                            }

                            var output = Encoding.UTF8.GetBytes(respMsg);
                            await aServer.WriteAsync(output, 0, output.Length, aToken).ConfigureAwait(false);

                            foreach (var m in respMsgs)
                            {
                                if (m is Error && (m as Error).ErrorCode == Error.ErrorClass.ERROR_PING && aServer.IsConnected)
                                {
                                    aServer.Close();
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.Error(e.Message, true);
                    try
                    {
                        aServer.Close();
                    }
                    catch
                    {
                        // noop
                    }
                }
                finally
                {
                    buttplugServer.MessageReceived -= MsgReceived;
                    await buttplugServer.ShutdownAsync().ConfigureAwait(false);
                    buttplugServer = null;
                    _connections.TryDequeue(out var stashed);
                    while (stashed != aServer && _connections.Any())
                    {
                        _connections.Enqueue(stashed);
                        _connections.TryDequeue(out stashed);
                    }

                    aServer.Close();
                    aServer.Dispose();
                    aServer = null;
                    ConnectionClosed?.Invoke(this, new IPCConnectionEventArgs());
                }
            }
        }

        public void StopServer()
        {
            if (!_cancellation.IsCancellationRequested)
            {
                try
                {
                    _cancellation.Cancel();
                }
                catch
                {
                }
            }
        }

        public void Disconnect()
        {
            foreach (var conn in _connections)
            {
                conn.Close();
            }
        }

        ~ButtplugIPCServer()
        {
            StopServer();
        }
    }
}
