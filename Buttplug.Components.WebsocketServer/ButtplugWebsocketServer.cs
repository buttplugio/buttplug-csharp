using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;
using Buttplug.Server;
using JetBrains.Annotations;
using vtortola.WebSockets;
using static Buttplug.Core.Messages.Error;

namespace Buttplug.Components.WebsocketServer
{
    public class ButtplugWebsocketServer
    {
        [NotNull]
        private WebSocketListener _server;

        [NotNull]
        private IButtplugServerFactory _factory;

        [NotNull]
        private IButtplugLogManager _logManager;

        [NotNull]
        private IButtplugLog _logger;

        [CanBeNull]
        public EventHandler<UnhandledExceptionEventArgs> OnException;

        [CanBeNull]
        public EventHandler<ConnectionEventArgs> ConnectionAccepted;

        [CanBeNull]
        public EventHandler<ConnectionEventArgs> ConnectionUpdated;

        [CanBeNull]
        public EventHandler<ConnectionEventArgs> ConnectionClosed;

        [NotNull]
        private ConcurrentDictionary<string, WebSocket> _connections = new ConcurrentDictionary<string, WebSocket>();

        public void StartServer([NotNull] IButtplugServerFactory aFactory, int aPort = 12345, bool aSecure = false, string aHostname = "localhost")
        {
            CancellationTokenSource cancellation = new CancellationTokenSource();
            _factory = aFactory;

            _logManager = new ButtplugLogManager();
            _logger = _logManager.GetLogger(this.GetType());

            var endpoint = new IPEndPoint(IPAddress.Any, aPort);
            _server = new WebSocketListener(endpoint);
            var rfc6455 = new vtortola.WebSockets.Rfc6455.WebSocketFactoryRfc6455(_server);
            _server.Standards.RegisterStandard(rfc6455);
            if (aSecure)
            {
                var cert = CertUtils.GetCert("Buttplug", aHostname);
                _server.ConnectionExtensions.RegisterExtension(new WebSocketSecureConnectionExtension(cert));
            }

            _server.Start();

            Task.Run(() => AcceptWebSocketClientsAsync(_server, cancellation.Token));
        }

        private async Task AcceptWebSocketClientsAsync(WebSocketListener aServer, CancellationToken aToken)
        {
            while (!aToken.IsCancellationRequested)
            {
                try
                {
                    var ws = await aServer.AcceptWebSocketAsync(aToken).ConfigureAwait(false);
                    if (ws != null)
                    {
                        Task.Run(() => HandleConnectionAsync(ws, aToken));
                    }
                }
                catch (Exception aEx)
                {
                    OnException?.Invoke(this, new UnhandledExceptionEventArgs(aEx, false));
                }
            }
        }

        private async Task HandleConnectionAsync(WebSocket ws, CancellationToken cancellation)
        {
            if (!_connections.IsEmpty)
            {
                try
                {
                    ws.WriteString(new ButtplugJsonMessageParser(_logManager).Serialize(_logger.LogErrorMsg(
                        ButtplugConsts.SystemMsgId, ErrorClass.ERROR_INIT, "WebSocketServer already in use!")));
                    ws.Close();
                }
                catch
                {
                    // noop
                }
                finally
                {
                    ws.Dispose();
                }

                return;
            }

            var remoteId = ws.RemoteEndpoint.ToString();
            _connections.AddOrUpdate(remoteId, ws, (oldWs, newWs) => newWs);
            ConnectionAccepted?.Invoke(this, new ConnectionEventArgs(remoteId));

            var buttplug = _factory.GetServer();

            EventHandler<MessageReceivedEventArgs> msgReceived = (aObject, aEvent) =>
            {
                var msg = buttplug.Serialize(aEvent.Message);
                try
                {
                    if (ws != null && ws.IsConnected)
                    {
                        ws.WriteString(msg);
                    }

                    if (aEvent.Message is Error && (aEvent.Message as Error).ErrorCode == Error.ErrorClass.ERROR_PING && ws != null && ws.IsConnected)
                    {
                        ws.Close();
                    }
                }
                catch (WebSocketException e)
                {
                    // Probably means we're repling to a message we recieved just before shutdown.
                    _logger.Error(e.Message, true);
                }
            };

            buttplug.MessageReceived += msgReceived;

            EventHandler<MessageReceivedEventArgs> clientConnected = (aObject, aEvent) =>
            {
                var msg = aEvent.Message as RequestServerInfo;
                var clientName = msg?.ClientName ?? "Unknown client";
                ConnectionUpdated?.Invoke(this, new ConnectionEventArgs(remoteId, clientName));
            };

            buttplug.ClientConnected += clientConnected;

            try
            {
                while (ws.IsConnected && !cancellation.IsCancellationRequested)
                {
                    var msg = await ws.ReadStringAsync(cancellation).ConfigureAwait(false);
                    if (msg != null)
                    {
                        var respMsgs = await buttplug.SendMessage(msg);
                        var respMsg = buttplug.Serialize(respMsgs);
                        await ws.WriteStringAsync(respMsg, cancellation);

                        foreach (var m in respMsgs)
                        {
                            if (m is Error && (m as Error).ErrorCode == Error.ErrorClass.ERROR_PING && ws != null && ws.IsConnected)
                            {
                                ws.Close();
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
                    ws.Close();
                }
                catch
                {
                    // noop
                }
            }
            finally
            {
                buttplug.MessageReceived -= msgReceived;
                await buttplug.Shutdown();
                buttplug = null;
                ws.Dispose();
                _connections.TryRemove(remoteId, out _);
                ConnectionClosed?.Invoke(this, new ConnectionEventArgs(remoteId));
            }
        }

        public void StopServer()
        {
            _server?.Stop();
        }
    }
}
