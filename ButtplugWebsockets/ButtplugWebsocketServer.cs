using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using JetBrains.Annotations;
using vtortola.WebSockets;

namespace ButtplugWebsockets
{
    public class ButtplugWebsocketServer
    {
        private WebSocketListener _server;
        [NotNull]
        private IButtplugServiceFactory _factory;

        public void StartServer([NotNull] IButtplugServiceFactory aFactory, int aPort = 12345, bool aSecure = false)
        {
            CancellationTokenSource cancellation = new CancellationTokenSource();
            _factory = aFactory;

            var endpoint = new IPEndPoint(IPAddress.Any, aPort);
            _server = new WebSocketListener(endpoint);
            var rfc6455 = new vtortola.WebSockets.Rfc6455.WebSocketFactoryRfc6455(_server);
            _server.Standards.RegisterStandard(rfc6455);
            if (aSecure)
            {
                var cert = CertUtils.GetCert("Buttplug");
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
                    // TODO: Actually log here.
                }
            }
        }

        private async Task HandleConnectionAsync(WebSocket ws, CancellationToken cancellation)
        {
            var buttplug = _factory.GetService();

            EventHandler<Buttplug.Core.MessageReceivedEventArgs> msgReceived = (aObject, aEvent) =>
            {
                var msg = buttplug.Serialize(aEvent.Message);
                try
                {
                    if (ws != null && ws.IsConnected)
                    {
                        ws.WriteString(msg);
                    }
                }
                catch (WebSocketException)
                {
                    // Log? - probably means we're repling to a message we recieved just before shutdown.
                }
            };

            buttplug.MessageReceived += msgReceived;

            try
            {
                while (ws.IsConnected && !cancellation.IsCancellationRequested)
                {
                    var msg = await ws.ReadStringAsync(cancellation).ConfigureAwait(false);
                    if (msg != null)
                    {
                        var respMsg = buttplug.Serialize(await buttplug.SendMessage(msg));
                        await ws.WriteStringAsync(respMsg, cancellation);
                    }
                }
            }
            catch (Exception)
            {
                // TODO Log here.
                try { ws.Close(); }
                catch { }
            }
            finally
            {
                buttplug.MessageReceived -= msgReceived;
                buttplug = null;
                ws.Dispose();
            }
        }

        public void StopServer()
        {
            _server?.Stop();
        }
    }
}
