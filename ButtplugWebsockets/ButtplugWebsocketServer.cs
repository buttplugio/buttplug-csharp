using System;
using System.Text;
using Buttplug.Core;
using JetBrains.Annotations;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace ButtplugWebsockets
{
    public class ButtplugWebsocketServer
    {
        [NotNull]
        private WebSocketServer _wsServer;

        public ButtplugWebsocketServer()
        {
            _wsServer = new WebSocketServer("ws://localhost:12345");
            _wsServer.RemoveWebSocketService("/buttplug");
        }

        public void StartServer([NotNull] ButtplugService aService)
        {
            _wsServer.AddWebSocketService<ButtplugWebsocketServerBehavior>("/buttplug", () => new ButtplugWebsocketServerBehavior(aService));
            _wsServer.Start();
        }

        public void StopServer()
        {
            _wsServer.Stop();
            _wsServer.RemoveWebSocketService("/buttplug");            
        }
    }

}
