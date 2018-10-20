// <copyright file="ButtplugClientTestConnector.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

// Test file, disable ConfigureAwait checking.
// ReSharper disable ConsiderUsingConfigureAwait

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using NUnit.Framework;

namespace Buttplug.Client.Test
{
    public class ButtplugClientTestConnector : IButtplugClientConnector
    {
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public event EventHandler<ButtplugExceptionEventArgs> InvalidMessageReceived;

        public event EventHandler Disconnected;

        public IButtplugLogManager LogManager { private get; set; }

        public bool Connected => _connected;

        private bool _connected = false;

        private Dictionary<Type, ButtplugMessage> _messageResponse;

        public ButtplugClientTestConnector()
        {
            _messageResponse = new Dictionary<Type, ButtplugMessage>();
            _messageResponse.Add(typeof(RequestServerInfo), new ServerInfo("Test Server", ButtplugConsts.CurrentSpecVersion, 0));
            _messageResponse.Add(typeof(RequestDeviceList), new DeviceList(new DeviceMessageInfo[0], ButtplugConsts.DefaultMsgId));
        }

        public void SetMessageResponse<T>(ButtplugMessage aMsg)
            where T : ButtplugMessage
        {
            _messageResponse.Remove(typeof(T));
            _messageResponse.Add(typeof(T), aMsg);
        }

        public void SendServerMessage(ButtplugMessage aMsg)
        {
            MessageReceived?.Invoke(this, new MessageReceivedEventArgs(aMsg));
        }

        public Task ConnectAsync(CancellationToken aToken = default(CancellationToken))
        {
            _connected = true;
            return Task.CompletedTask;
        }

        public Task DisconnectAsync(CancellationToken aToken = default(CancellationToken))
        {
            _connected = false;
            return Task.CompletedTask;
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