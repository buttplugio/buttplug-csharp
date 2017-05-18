using System.Text;
using Buttplug.Core;
using WebSocketSharp;
using WebSocketSharp.Server;
using NLog;

namespace ButtplugWebsocketServer
{
    public class ButtplugWebsocketServer : WebSocketBehavior
    {
        private readonly ButtplugService _buttplug;
        private NLog.Logger _bpLogger;
        public ButtplugWebsocketServer()
        {
            _bpLogger = LogManager.GetCurrentClassLogger();
            _buttplug = new ButtplugService();
            _buttplug.MessageReceived += OnMessageReceived;
        }

        protected override async void OnMessage(MessageEventArgs e)
        {
            base.OnMessage(e);
            var msg = _buttplug.Serialize(await _buttplug.SendMessage(e.Data));
            Sessions.Broadcast(Encoding.ASCII.GetBytes(msg));

        }

        public void OnMessageReceived(object o, MessageReceivedEventArgs e)
        {
            var msg = _buttplug.Serialize(e.Message);
            Sessions.Broadcast(Encoding.ASCII.GetBytes(msg));
        }
    }
}
