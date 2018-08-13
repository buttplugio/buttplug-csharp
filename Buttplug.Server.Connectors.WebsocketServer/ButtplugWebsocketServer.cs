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
using Buttplug.Server;
using JetBrains.Annotations;
using vtortola.WebSockets;
using vtortola.WebSockets.Rfc6455;
using static Buttplug.Core.Messages.Error;

namespace Buttplug.Server.Connectors.WebsocketServer
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

        private uint _maxConnections = 1;

        [NotNull]
        private CancellationTokenSource _cancellation;

        [CanBeNull]
        private Task _websocketTask;

        public bool IsConnected => _server != null;

        public async Task StartServer([NotNull] IButtplugServerFactory aFactory, uint maxConnections = 1, int aPort = 12345, bool aLoopBack = true, bool aSecure = false, string aHostname = "localhost")
        {
            _cancellation = new CancellationTokenSource();
            _factory = aFactory;

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
            await _server.StartAsync();

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
                        await Task.Run(() => HandleConnectionAsync(ws, aToken), aToken);
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
                    await ws.WriteStringAsync(new ButtplugJsonMessageParser(_logManager).Serialize(_logger.LogErrorMsg(
                        ButtplugConsts.SystemMsgId, ErrorClass.ERROR_INIT, "WebSocketServer already in use!"), 0));
                    await ws.CloseAsync();
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
            _connections.TryRemove(remoteId, out var closews);
        }

        public async void StopServer()
        {
            if (_websocketTask == null)
            {
                return;
            }
            _cancellation.Cancel();
            await _websocketTask;
        }

        public async Task Disconnect(string remoteId = null)
        {
            if (remoteId == null)
            {
                foreach (var conn in _connections.Values)
                {
                    await conn.CloseAsync();
                }

                return;
            }

            if (_connections.TryGetValue(remoteId, out WebSocket ws))
            {
                await ws.CloseAsync();
            }
        }
    }
}
