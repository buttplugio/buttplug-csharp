using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Messages;
using Buttplug.Server;
using JetBrains.Annotations;
using vtortola.WebSockets;

namespace Buttplug.Components.WebsocketServer
{
    public class ButtplugWebsocketServer
    {
        private WebSocketListener _server;
        [NotNull]
        private IButtplugServerFactory _factory;

        [CanBeNull]
        public EventHandler<UnhandledExceptionEventArgs> OnException;

        public void StartServer([NotNull] IButtplugServerFactory aFactory, int aPort = 12345, bool aSecure = false)
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
                    OnException?.Invoke(this, new UnhandledExceptionEventArgs(aEx, false));
                }
            }
        }

        private async Task HandleConnectionAsync(WebSocket ws, CancellationToken cancellation)
        {
            var buttplug = _factory.GetServer();

            EventHandler<Buttplug.Core.MessageReceivedEventArgs> msgReceived = (aObject, aEvent) =>
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
            catch (Exception)
            {
                // TODO Log here.
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
                buttplug.Shutdown();
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
