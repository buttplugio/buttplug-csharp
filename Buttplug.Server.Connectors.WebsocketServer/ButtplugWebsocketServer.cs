// <copyright file="ButtplugWebsocketServer.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Logging;
using JetBrains.Annotations;
using vtortola.WebSockets;
using vtortola.WebSockets.Rfc6455;

namespace Buttplug.Server.Connectors.WebsocketServer
{
    public class ButtplugWebsocketServer
    {
        [CanBeNull]
        private WebSocketListener _server;

        [NotNull]
        private Func<ButtplugServer> _serverFactory;

        [NotNull]
        private IButtplugLogManager _logManager;

        [NotNull]
        private IButtplugLog _logger;

        [CanBeNull]
        public EventHandler<UnhandledExceptionEventArgs> OnException;

        [CanBeNull]
        public EventHandler<ConnectionEventArgs> ConnectionAccepted;

        [CanBeNull]
        public EventHandler<ConnectionEventArgs> ConnectionClosed;

        [NotNull]
        private readonly ConcurrentDictionary<string, WebSocket> _connections = new ConcurrentDictionary<string, WebSocket>();

        private uint _maxConnections = 1;

        [NotNull]
        private CancellationTokenSource _cancellation;

        [CanBeNull]
        private Task _websocketTask;

        public bool Connected => _server != null;

        public async Task StartServerAsync([NotNull] Func<ButtplugServer> aFactory, uint maxConnections = 1, int aPort = 12345, bool aLoopBack = true, bool aSecure = false, string aHostname = "localhost")
        {
            _cancellation = new CancellationTokenSource();
            _serverFactory = aFactory;

            _maxConnections = maxConnections;

            _logManager = new ButtplugLogManager();
            _logger = _logManager.GetLogger(GetType());

            var endpoint = new IPEndPoint(aLoopBack ? IPAddress.Loopback : IPAddress.Any, aPort);

            var options = new WebSocketListenerOptions();
            options.Standards.RegisterRfc6455();
            if (aSecure)
            {
                var cert = CertUtils.GetCert("Buttplug", aHostname);
                options.ConnectionExtensions.RegisterSecureConnection(cert);
            }

            _server = new WebSocketListener(endpoint, options);
            await _server.StartAsync().ConfigureAwait(false);

            _websocketTask = Task.Run(() => AcceptWebSocketClientsAsync(_server, _cancellation.Token));
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
                        await Task.Run(() => HandleConnectionAsync(ws, aToken), aToken).ConfigureAwait(false);
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
            if (_connections.Count == _maxConnections)
            {
                try
                {
                    await ws.WriteStringAsync(new ButtplugJsonMessageParser(_logManager).Serialize(new ButtplugHandshakeException("WebSocketServer already in use!").ButtplugErrorMessage, 0)).ConfigureAwait(false);
                    await ws.CloseAsync().ConfigureAwait(false);
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

            var session = new ButtplugWebsocketServerSession(_logManager, _serverFactory(), ws, _cancellation);
            session.ConnectionAccepted += (aObj, aEventArgs) =>
            {
                ConnectionAccepted?.Invoke(this, aEventArgs);
            };
            session.ConnectionClosed += (aObj, aEventArgs) =>
            {
                ConnectionClosed?.Invoke(this, aEventArgs);
            };
            await session.RunServerSession().ConfigureAwait(false);
            _connections.TryRemove(remoteId, out var closeWebSockets);
        }

        public async Task StopServerAsync()
        {
            if (_websocketTask == null)
            {
                return;
            }
            _cancellation.Cancel();
            await _websocketTask.ConfigureAwait(false);
            await _server.StopAsync().ConfigureAwait(false);
            _server = null;
        }

        public async Task DisconnectAsync(string remoteId = null)
        {
            if (remoteId == null)
            {
                foreach (var conn in _connections.Values)
                {
                    await conn.CloseAsync().ConfigureAwait(false);
                }

                return;
            }

            if (_connections.TryGetValue(remoteId, out WebSocket ws))
            {
                await ws.CloseAsync().ConfigureAwait(false);
            }
        }
    }
}
