using System;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;

namespace Buttplug.Client.Test
{
    public class ButtplugClientTestConnector : IButtplugClientConnector
    {
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public event EventHandler<ButtplugClientException> InvalidMessageReceived;

        public event EventHandler Disconnected;

        public IButtplugLogManager LogManager { private get; set; }

        public bool Connected => _connected;

        private bool _connected = false;

        public virtual async Task ConnectAsync(CancellationToken aToken = default(CancellationToken))
        {
            _connected = true;
        }

        public virtual async Task DisconnectAsync(CancellationToken aToken = default(CancellationToken))
        {
            _connected = false;
        }

        public virtual async Task<ButtplugMessage> SendAsync(ButtplugMessage aMsg, CancellationToken aToken = default(CancellationToken))
        {
            return new Ok(aMsg.Id);
        }
    }
}