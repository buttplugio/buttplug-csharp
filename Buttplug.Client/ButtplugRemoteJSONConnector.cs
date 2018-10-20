// <copyright file="ButtplugRemoteJSONConnector.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace Buttplug.Client
{
    public class ButtplugRemoteJSONConnector
    {
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public event EventHandler<ButtplugExceptionEventArgs> InvalidMessageReceived;

        public IButtplugLogManager LogManager
        {
            set
            {
                _logManager = value;
                _logger = _logManager.GetLogger(GetType());
            }
        }

        [CanBeNull]
        protected IButtplugLogManager _logManager;

        [CanBeNull]
        protected IButtplugLog _logger;
        private readonly ButtplugConnectorJSONParser _jsonSerializer = new ButtplugConnectorJSONParser();
        private readonly ButtplugConnectorMessageSorter _msgSorter = new ButtplugConnectorMessageSorter();

        protected Tuple<string, Task<ButtplugMessage>> PrepareMessage(ButtplugMessage aMsg)
        {
            var promise = _msgSorter.PrepareMessage(aMsg);
            var jsonMsg = _jsonSerializer.Serialize(aMsg);
            return new Tuple<string, Task<ButtplugMessage>>(jsonMsg, promise);
        }

        protected void Shutdown()
        {
            _msgSorter.Shutdown();
        }

        protected void ReceiveMessages(string aJSONMsg)
        {
            IEnumerable<ButtplugMessage> msgs;
            try
            {
                msgs = _jsonSerializer.Deserialize(aJSONMsg);
            }
            catch (ButtplugMessageException e)
            {
                InvalidMessageReceived?.Invoke(this, new ButtplugExceptionEventArgs(e));
                return;
            }

            foreach (var msg in msgs)
            {
                if (msg.Id == 0)
                {
                    MessageReceived?.Invoke(this, new MessageReceivedEventArgs(msg));
                    continue;
                }

                try
                {
                    _msgSorter.CheckMessage(msg, _logger);
                }
                catch (ButtplugMessageException e)
                {
                    InvalidMessageReceived?.Invoke(this, new ButtplugExceptionEventArgs(e));
                }
            }
        }
    }
}