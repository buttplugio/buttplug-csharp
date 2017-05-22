using System.Text;
using Buttplug.Core;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace ButtplugWebsocketServer
{
    public class ButtplugWebsocketServer : WebSocketBehavior
    {
        private readonly ButtplugService _buttplug;
        public ButtplugWebsocketServer()
        {
            _buttplug = new ButtplugService();
            _buttplug.MessageReceived += OnMessageReceived;
        }

        protected override async void OnMessage(MessageEventArgs e)
        {
            base.OnMessage(e);
            var msg = ButtplugService.Serialize(await _buttplug.SendMessage(e.Data));
            Sessions?.Broadcast(Encoding.ASCII.GetBytes(msg));

        }

        private void OnMessageReceived(object o, MessageReceivedEventArgs e)
        {
            var msg = ButtplugService.Serialize(e.Message);
            Sessions?.Broadcast(Encoding.ASCII.GetBytes(msg));
        }
    }
}
