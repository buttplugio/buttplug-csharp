// <copyright file="ButtplugClientTestJSONConnector.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

// Test file, disable ConfigureAwait checking.
// ReSharper disable ConsiderUsingConfigureAwait

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;
using NUnit.Framework;

namespace Buttplug.Client.Test
{
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Test classes can skip documentation requirements")]
    public class ButtplugClientTestJSONConnector : ButtplugRemoteJSONConnector, IButtplugClientConnector
    {
        public event EventHandler Disconnected;

        public bool Connected { get; private set; }

        private Dictionary<Type, ButtplugMessage> _messageResponse;

        public ButtplugClientTestJSONConnector()
        {
            _messageResponse = new Dictionary<Type, ButtplugMessage>();
            SetMessageResponse<RequestServerInfo>(new ServerInfo("Test Server", ButtplugConsts.CurrentSpecVersion, 0));
            SetMessageResponse<RequestDeviceList>(new DeviceList(new DeviceMessageInfo[0], ButtplugConsts.DefaultMsgId));
        }

        public void SetMessageResponse<T>(ButtplugMessage msg)
            where T : ButtplugMessage
        {
            _messageResponse.Remove(typeof(T));
            _messageResponse.Add(typeof(T), msg);
        }

        public void SendServerMessage(string msgString)
        {
            ReceiveMessages(msgString);
        }

        public Task ConnectAsync(CancellationToken token = default(CancellationToken))
        {
            Connected = true;
            return Task.CompletedTask;
        }

        public Task DisconnectAsync(CancellationToken token = default(CancellationToken))
        {
            Connected = false;
            return Task.CompletedTask;
        }

        public Task<ButtplugMessage> SendAsync(ButtplugMessage msg, CancellationToken token = default(CancellationToken))
        {
            var msg = _messageResponse[msg.GetType()];
            if (msg == null)
            {
                Assert.Fail($"Don't have a message to respond to {msg.GetType()} with.");
            }

            msg.Id = msg.Id;
            return Task.FromResult(msg);
        }
    }
}