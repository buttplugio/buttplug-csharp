using System;
using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;
using Buttplug.Server;
using JetBrains.Annotations;
using static Buttplug.Core.Messages.Error;

namespace Buttplug.Components.IPCServer
{
    public class ButtplugIPCServer
    {
        [NotNull]
        private IButtplugServerFactory _factory;

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

        public bool IsConnected => _acceptTask?.Status == TaskStatus.Running;

        public void StartServer([NotNull] IButtplugServerFactory aFactory, string aPipeName = "ButtplugPipe")
        {
            _cancellation = new CancellationTokenSource();
            _factory = aFactory;

            _logManager = new ButtplugLogManager();
            _logger = _logManager.GetLogger(this.GetType());

            _acceptTask = new Task(() => { ConnectionAccepter(aPipeName, _cancellation.Token); }, _cancellation.Token, TaskCreationOptions.LongRunning);
            _acceptTask.Start();
        }

        private async void ConnectionAccepter(string aPipeName, CancellationToken aCancellationToken)
        {
            while (!aCancellationToken.IsCancellationRequested)
            {
                var pipeServer = new NamedPipeServerStream(aPipeName, PipeDirection.InOut, 10, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                await pipeServer.WaitForConnectionAsync(aCancellationToken);
                if (!pipeServer.IsConnected)
                {
                    continue;
                }

                var temp = pipeServer;
                var reader = new Task(() => { AcceptIPCClientsAsync(temp, _cancellation.Token); }, _cancellation.Token,
                    TaskCreationOptions.LongRunning);
                reader.Start();
                reader.Wait(aCancellationToken);
            }
        }

        private async void AcceptIPCClientsAsync(NamedPipeServerStream aServer, CancellationToken aToken)
        {
            if (!_connections.IsEmpty)
            {
                try
                {
                    var output = Encoding.UTF8.GetBytes(new ButtplugJsonMessageParser(_logManager).Serialize(_logger.LogErrorMsg(
                        ButtplugConsts.SystemMsgId, ErrorClass.ERROR_INIT, "WebSocketServer already in use!"), 0));
                    await aServer.WriteAsync(output, 0, output.Length, aToken);
                    aServer.Close();
                }
                catch
                {
                    // no-op?
                }
                finally
                {
                    // aServer.Dispose();
                }

                return;
            }

            _connections.Enqueue(aServer);
            ConnectionAccepted?.Invoke(this, new IPCConnectionEventArgs());

            var buttplugServer = _factory.GetServer();

            EventHandler<MessageReceivedEventArgs> msgReceived = (aObject, aEvent) =>
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
                    // Probably means we're repling to a message we recieved just before shutdown.
                    _logger.Error(e.Message, true);
                }
            };

            buttplugServer.MessageReceived += msgReceived;

            EventHandler<MessageReceivedEventArgs> clientConnected = (aObject, aEvent) =>
            {
                var msg = aEvent.Message as RequestServerInfo;
                var clientName = msg?.ClientName ?? "Unknown client";
                ConnectionUpdated?.Invoke(this, new IPCConnectionEventArgs(clientName));
            };

            buttplugServer.ClientConnected += clientConnected;

            try
            {
                while (!aToken.IsCancellationRequested && aServer.IsConnected)
                {
                    var buffer = new byte[4096];
                    string msg = string.Empty;
                    var len = -1;
                    while (len < 0 || (len == buffer.Length && buffer[4095] != '\0'))
                    {
                        try
                        {
                            len = await aServer.ReadAsync(buffer, 0, buffer.Length, aToken);
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
                        var respMsgs = await buttplugServer.SendMessage(msg);
                        var respMsg = buttplugServer.Serialize(respMsgs);
                        if (respMsg == null)
                        {
                            continue;
                        }

                        var output = Encoding.UTF8.GetBytes(respMsg);
                        await aServer.WriteAsync(output, 0, output.Length, aToken);

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
                buttplugServer.MessageReceived -= msgReceived;
                await buttplugServer.Shutdown();
                buttplugServer = null;
                _connections.TryDequeue(out var stashed);
                while (stashed != aServer && _connections.Any())
                {
                    _connections.Enqueue(stashed);
                    _connections.TryDequeue(out stashed);
                }

                aServer.Close();
                // aServer.Dispose();
                // aServer = null;
                ConnectionClosed?.Invoke(this, new IPCConnectionEventArgs());
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
