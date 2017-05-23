using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buttplug.Core;
using JetBrains.Annotations;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace ButtplugWebsockets
{
    public class ButtplugWebsocketServerBehavior : WebSocketBehavior
    {
        [NotNull]
        private readonly ButtplugService _buttplug;

        public ButtplugWebsocketServerBehavior()
            : this(null)
        {
            
        }

        public ButtplugWebsocketServerBehavior(ButtplugService aService)
        {
            if (aService == null)
            {
                return;
            }
            _buttplug = aService;
            _buttplug.MessageReceived += OnMessageReceived;
        }

        protected override void OnClose(CloseEventArgs e)
        {
            base.OnClose(e);
            _buttplug.MessageReceived -= OnMessageReceived;
        }

        protected override async void OnMessage(MessageEventArgs e)
        {
            base.OnMessage(e);
            var msg = ButtplugService.Serialize(await _buttplug.SendMessage(e.Data));
            Sessions?.Broadcast(Encoding.ASCII.GetBytes(msg));
        }

        private void OnMessageReceived(object aObj, MessageReceivedEventArgs e)
        {
            var msg = ButtplugService.Serialize(e.Message);
            Sessions?.Broadcast(Encoding.ASCII.GetBytes(msg));
        }
    }
}
