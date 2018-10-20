using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;
using NUnit.Framework;

namespace Buttplug.Client.Test
{
    public class ButtplugClientTestJSONConnector : ButtplugRemoteJSONConnector, IButtplugClientConnector
    {
        public event EventHandler Disconnected;

        public bool Connected => _connected;

        private bool _connected = false;

        private Dictionary<Type, ButtplugMessage> _messageResponse;

        public ButtplugClientTestJSONConnector()
        {
            _messageResponse = new Dictionary<Type, ButtplugMessage>();
            SetMessageResponse<RequestServerInfo>(new ServerInfo("Test Server", ButtplugConsts.CurrentSpecVersion, 0));
            SetMessageResponse<RequestDeviceList>(new DeviceList(new DeviceMessageInfo[0], ButtplugConsts.DefaultMsgId));
        }

        public void SetMessageResponse<T>(ButtplugMessage aMsg)
            where T : ButtplugMessage
        {
            _messageResponse.Remove(typeof(T));
            _messageResponse.Add(typeof(T), aMsg);
        }

        public void SendServerMessage(string aMsgString)
        {
            ReceiveMessages(aMsgString);
        }

        public async Task ConnectAsync(CancellationToken aToken = default(CancellationToken))
        {
            _connected = true;
        }

        public async Task DisconnectAsync(CancellationToken aToken = default(CancellationToken))
        {
            _connected = false;
        }

        public async Task<ButtplugMessage> SendAsync(ButtplugMessage aMsg, CancellationToken aToken = default(CancellationToken))
        {
            var msg = _messageResponse[aMsg.GetType()];
            if (msg == null)
            {
                Assert.Fail($"Don't have a message to respond to {aMsg.GetType()} with.");
            }

            msg.Id = aMsg.Id;
            return msg;
        }
    }
}