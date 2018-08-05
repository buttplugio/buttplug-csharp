using System;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using JetBrains.Annotations;

namespace Buttplug.Client
{
    public abstract class ButtplugRemoteJSONConnector
    {
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        private readonly ButtplugConnectorJSONParser _jsonSerializer = new ButtplugConnectorJSONParser();
        private readonly ButtplugConnectorMessageSorter _msgSorter = new ButtplugConnectorMessageSorter();

        protected Tuple<string, Task<ButtplugMessage>> PrepareMessage(ButtplugMessage aMsg)
        {
            var promise = _msgSorter.PrepareMessage(aMsg);
            var jsonMsg = _jsonSerializer.Serialize(aMsg);
            return new Tuple<string, Task<ButtplugMessage>>(jsonMsg, promise);
        }

        protected void ReceiveMessages(string aJSONMsg)
        {
            var msgs = _jsonSerializer.Deserialize(aJSONMsg);
            foreach (var msg in msgs)
            {
                if (msg.Id == 0)
                {
                    MessageReceived.Invoke(this, new MessageReceivedEventArgs(msg));
                    continue;
                }

                if (!_msgSorter.CheckMessage(msg))
                {
                    // TODO throw an error here?
                }
            }
        }
    }
}