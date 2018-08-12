// <copyright file="ButtplugRemoteJSONConnector.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Threading.Tasks;
using Buttplug.Core;

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
                    MessageReceived?.Invoke(this, new MessageReceivedEventArgs(msg));
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