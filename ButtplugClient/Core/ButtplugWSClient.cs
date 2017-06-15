using Buttplug.Core;
using Buttplug.Messages;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Threading;
using System.Text;

namespace ButtplugClient.Core
{
    public class ButtplugWSClient
    {
        [NotNull]
        private readonly ButtplugJsonMessageParser _parser;
        [CanBeNull]
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        [NotNull]
        private readonly IButtplugLog _bpLogger;
        [NotNull]
        private readonly IButtplugLogManager _bpLogManager;

        private readonly string _clientName;
        private readonly uint _messageSchemaVersion;
        private bool _receivedServerInfo;
        private ClientWebSocket _ws;

        public ButtplugWSClient(string aClientName)
        {
            _clientName = aClientName;
            _bpLogManager = new ButtplugLogManager();
            _bpLogger = _bpLogManager.GetLogger(GetType());
            _parser = new ButtplugJsonMessageParser(_bpLogManager);
            _bpLogger.Trace("Finished setting up ButtplugClient");
        }

        public async Task Connect(Uri aURL)
        {
            _ws = new ClientWebSocket();
            await _ws.ConnectAsync(aURL, CancellationToken.None);
            var res = await SendMessage(new RequestServerInfo(_clientName));
            if( res.Length == 1 && res[0] is ServerInfo )
            {
                // parse the server info
            }
        }
        public async Task<ButtplugMessage[]> SendMessage(ButtplugMessage aMsg)
        {
            List<ButtplugMessage> res = new List<ButtplugMessage>();
            var output = this.Serialize(aMsg);
            var segment1 = new ArraySegment<byte>(Encoding.UTF8.GetBytes(output));
            await _ws.SendAsync(segment1, WebSocketMessageType.Text, true, CancellationToken.None);

            var buffer = new byte[1024];
            var segment2 = new ArraySegment<byte>(buffer);
            var result = await _ws.ReceiveAsync(segment2, CancellationToken.None);
            var input = Encoding.UTF8.GetString(buffer, 0, result.Count);
            res.AddRange( Deserialize(input) );
            return res.ToArray();
        }

        public string Serialize(ButtplugMessage aMsg)
        {
            return _parser.Serialize(aMsg);
        }

        public string Serialize(ButtplugMessage[] aMsgs)
        {
            return _parser.Serialize(aMsgs);
        }

        public ButtplugMessage[] Deserialize(string aMsg)
        {
            return _parser.Deserialize(aMsg);
        }
    }
}

