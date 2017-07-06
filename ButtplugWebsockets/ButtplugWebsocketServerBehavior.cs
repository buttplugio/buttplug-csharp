using System;
using System.Linq;
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
            set
            {
                if (_buttplug != null)
                {
                    throw new AccessViolationException("Service already set!");
                }

                _buttplug = value ?? throw new ArgumentNullException();
                _buttplug.MessageReceived += OnMessageReceived;
            }
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
                    var msg = _buttplug.Serialize(new Error("WebSocketServer already in use!", Buttplug.Messages.Error.ErrorClass.ERROR_INIT, ButtplugConsts.SystemMsgId));
                    try
                    {
                        Send(msg);
                    }
                    catch (InvalidOperationException)
                    {
                        // noop - likely already disconnected
                    }

                    Sessions?.CloseSession(ID);
                }
            }
        }

        protected override async void OnClose([NotNull] CloseEventArgs aEvent)
        {
            base.OnClose(aEvent);
            _buttplug.MessageReceived -= OnMessageReceived;
            await _buttplug.SendMessage(new StopAllDevices());
            _buttplug = null;
        }

        protected override async void OnMessage([NotNull] MessageEventArgs aEvent)
        {
            base.OnMessage(aEvent);
            var msg = _buttplug.Serialize(await _buttplug.SendMessage(aEvent.Data));
            try
            {
                Send(msg);
            }
            catch (InvalidOperationException)
            {
                // noop - likely already disconnected
            }
        }

        private void OnMessageReceived(object aObj, [NotNull] MessageReceivedEventArgs aEvent)
        {
            var msg = _buttplug.Serialize(aEvent.Message);
            try
            {
                Send(msg);
            }
            catch (InvalidOperationException)
            {
                // noop - likely already disconnected
            }

            var error = aEvent.Message as Error;
            if (error != null && error.ErrorCode == Buttplug.Messages.Error.ErrorClass.ERROR_PING)
            {
                Sessions?.CloseSession(ID);
            }
        }
    }
}
