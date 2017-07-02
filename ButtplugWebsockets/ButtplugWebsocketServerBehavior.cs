using Buttplug.Core;
using Buttplug.Messages;
using System;
using System.Linq;
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

        protected override void OnOpen()
        {
            base.OnOpen();
            if (Sessions != null)
            {
                var ids = Sessions.ActiveIDs.ToList();
                ids.Remove(ID);
                if (ids.Any())
                {
                    var msg = _buttplug.Serialize(new Error("WebSocketServer already in use!", Buttplug.Messages.Error.ErrorClass.ERROR_INIT, ButtplugConsts.SYSTEM_MSG_ID));
                    try
                    {
                        Send(msg);
                    }
                    catch(InvalidOperationException)
                    {
                        // noop - likely already disconnected
                    }

                    Sessions?.CloseSession(ID);
                }
            }
        }

        protected override async void OnClose(CloseEventArgs e)
        {
            base.OnClose(e);
            _buttplug.MessageReceived -= OnMessageReceived;
            await _buttplug.SendMessage(new StopAllDevices());
            _buttplug = null;
        }

        protected override async void OnMessage(MessageEventArgs e)
        {
            base.OnMessage(e);
            var msg = _buttplug.Serialize(await _buttplug.SendMessage(e.Data));
            try
            {
                Send(msg);
            }
            catch (InvalidOperationException)
            {
                // noop - likely already disconnected
            }
        }

        private void OnMessageReceived(object aObj, MessageReceivedEventArgs e)
        {
            var msg = _buttplug.Serialize(e.Message);
            try
            {
                Send(msg);
            }
            catch (InvalidOperationException)
            {
                // noop - likely already disconnected
            }
            if (e.Message is Error && ((Error)e.Message).ErrorCode == Buttplug.Messages.Error.ErrorClass.ERROR_PING)
            {
                Sessions?.CloseSession(ID);
            }
        }
    }
}
