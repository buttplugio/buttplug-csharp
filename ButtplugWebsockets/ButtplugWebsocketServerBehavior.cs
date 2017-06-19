using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Messages;
using JetBrains.Annotations;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace ButtplugWebsockets
{
    public class ButtplugWebsocketServerBehavior : WebSocketBehavior
    {
        private ButtplugService _buttplug;
        
        public ButtplugService Service
        {
            set {
                if (_buttplug != null)
                {
                    throw new AccessViolationException("Service already set!");
                }
                _buttplug = value;
                _buttplug.MessageReceived += OnMessageReceived;
            }
            private get { return _buttplug; }
        }

        public ButtplugWebsocketServerBehavior()
        {

        }

        protected override async void OnClose(CloseEventArgs e)
        {
            base.OnClose(e);
            _buttplug.MessageReceived -= OnMessageReceived;
            await _buttplug.SendMessage(new StopAllDevices());
        }

        protected override async void OnMessage(MessageEventArgs e)
        {
            base.OnMessage(e);
            var msg = _buttplug.Serialize(await _buttplug.SendMessage(e.Data));
            Sessions?.Broadcast(msg);
        }

        private void OnMessageReceived(object aObj, MessageReceivedEventArgs e)
        {
            var msg = _buttplug.Serialize(e.Message);
            Sessions?.Broadcast(msg);
        }
    }
}
