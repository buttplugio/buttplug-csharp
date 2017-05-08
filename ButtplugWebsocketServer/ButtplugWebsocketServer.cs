using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buttplug.Core;
using WebSocketSharp;
using WebSocketSharp.Server;
using NLog;
using LanguageExt;

namespace ButtplugWebsocketServer
{
    public class ButtplugWebsocketServer : WebSocketBehavior
    {
        private readonly ButtplugService _buttplug;
        private NLog.Logger _bpLogger;
        public ButtplugWebsocketServer()
        {
            _bpLogger = LogManager.GetLogger(GetType().FullName);
            _buttplug = new ButtplugService();
            _buttplug.MessageReceived += OnMessageReceived;
        }

        protected override async void OnMessage(MessageEventArgs e)
        {
            base.OnMessage(e);
            await _buttplug.SendMessage(e.Data);
        }

        public void OnMessageReceived(object o, MessageReceivedEventArgs e)
        {
            var msg = ButtplugJsonMessageParser.Serialize(e.Message);
            msg.IfSome(x => Sessions.Broadcast(Encoding.ASCII.GetBytes(x)));
        }
    }
}
