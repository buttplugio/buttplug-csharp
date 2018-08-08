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

        [NotNull]
        private CancellationTokenSource _cancellation;

        public bool IsConnected => _server.IsStarted;

        public void StartServer([NotNull] IButtplugServerFactory aFactory, int aPort = 12345, bool aLoopBack = true, bool aSecure = false, string aHostname = "localhost")
        {
            _cancellation = new CancellationTokenSource();
            _factory = aFactory;

            _logManager = new ButtplugLogManager();
            _logger = _logManager.GetLogger(GetType());

            var endpoint = new IPEndPoint(aLoopBack ? IPAddress.Loopback : IPAddress.Any, aPort);
            _server = new WebSocketListener(endpoint);
            var rfc6455 = new vtortola.WebSockets.Rfc6455.WebSocketFactoryRfc6455(_server);
            _server.Standards.RegisterStandard(rfc6455);
            if (aSecure)
            {
                var cert = CertUtils.GetCert("Buttplug", aHostname);
                _server.ConnectionExtensions.RegisterExtension(new WebSocketSecureConnectionExtension(cert));
            }

            _server.Start();

            Task.Run(() => AcceptWebSocketClientsAsync(_server, _cancellation.Token));
        }

        private async Task AcceptWebSocketClientsAsync(WebSocketListener aServer, CancellationToken aToken)
        {
            while (!aToken.IsCancellationRequested)
            {
                WebSocket ws = null;
                try
                {
                    ws = await aServer.AcceptWebSocketAsync(aToken).ConfigureAwait(false);
                    if (ws != null)
                    {
                        Task.Run(() => HandleConnectionAsync(ws, aToken));
                    }
                }
                catch (Exception aEx)
                {
                    OnException?.Invoke(this, new UnhandledExceptionEventArgs(aEx, !(ws?.IsConnected ?? false)));
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
                        ButtplugConsts.SystemMsgId, ErrorClass.ERROR_INIT, "WebSocketServer already in use!"), 0));
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

            var session = new ButtplugWebsocketServerSession(_logManager, _factory.GetServer(), ws, _cancellation);
            await session.RunServerSession();
        }

        public void StopServer()
        {
            _cancellation.Cancel();
        }

        public void Disconnect(string remoteId = null)
        {
            if (remoteId == null)
            {
                foreach (var conn in _connections.Values)
                {
                    conn.Close();
                }

                return;
            }

            if (_connections.TryGetValue(remoteId, out WebSocket ws))
            {
                ws.Close();
            }
        }
    }
}
