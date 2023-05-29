using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Buttplug.Core.Messages;
using Buttplug.Core;

namespace Buttplug.Client
{
    internal class ButtplugClientMessageHandler
    {
        /// <summary>
        /// Connector to use for the client. Can be local (server embedded), IPC, Websocket, etc...
        /// </summary>
        private IButtplugClientConnector _connector;
        internal ButtplugClientMessageHandler(IButtplugClientConnector connector)
        {
            _connector = connector;
        }

        /// <summary>
        /// Sends a message to the server, and handles asynchronously waiting for the reply from the server.
        /// </summary>
        /// <param name="msg">Message to send.</param>
        /// <param name="token">Cancellation token, for cancelling action externally if it is not yet finished.</param>
        /// <returns>The response, which will derive from <see cref="ButtplugMessage"/>.</returns>
        public async Task<ButtplugMessage> SendMessageAsync(ButtplugMessage msg, CancellationToken token = default)
        {
            if (!_connector.Connected)
            {
                throw new ButtplugClientConnectorException("Client not connected.");
            }

            return await _connector.SendAsync(msg, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends a message, expecting a response of message type <see cref="Ok"/>.
        /// </summary>
        /// <param name="msg">Message to send.</param>
        /// <param name="token">Cancellation token, for cancelling action externally if it is not yet finished.</param>
        /// <returns>True if successful.</returns>
        public async Task SendMessageExpectOk(ButtplugMessage msg, CancellationToken token = default)
        {
            var result = await SendMessageAsync(msg, token).ConfigureAwait(false);
            switch (result)
            {
                case Ok _:
                    return;
                case Error err:
                    throw ButtplugException.FromError(err);
                default:
                    throw new ButtplugMessageException($"Message type {msg.Name} not handled by SendMessageExpectOk", msg.Id);
            }
        }
    }
}
