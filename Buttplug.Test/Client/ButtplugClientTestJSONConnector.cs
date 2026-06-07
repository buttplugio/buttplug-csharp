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

namespace Buttplug.Client.Test
{
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Test classes can skip documentation requirements")]
    public class ButtplugClientTestJSONConnector : ButtplugRemoteJSONConnector, IButtplugClientConnector
    {
        private readonly Dictionary<Type, ButtplugMessage> _messageResponse =
            new Dictionary<Type, ButtplugMessage>();

        public ButtplugClientTestJSONConnector()
        {
            SetMessageResponse<RequestServerInfo>(
                new ServerInfo(
                    "Test Server",
                    ButtplugConsts.ProtocolVersionMajor,
                    ButtplugConsts.ProtocolVersionMinor,
                    0));
            SetMessageResponse<RequestDeviceList>(
                new DeviceList(new Dictionary<uint, DeviceInfo>(), ButtplugConsts.DefaultMsgId));
        }

        public event EventHandler Disconnected;

        public bool Connected { get; private set; }

        public void SetMessageResponse<T>(ButtplugMessage msg)
            where T : ButtplugMessage
        {
            _messageResponse[typeof(T)] = msg;
        }

        public void SendServerMessage(string msgString)
        {
            ReceiveMessages(msgString);
        }

        public Task ConnectAsync(CancellationToken token = default)
        {
            Connected = true;
            return Task.CompletedTask;
        }

        public Task DisconnectAsync(CancellationToken token = default)
        {
            Connected = false;
            Disconnected?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }

        public Task<ButtplugMessage> SendAsync(ButtplugMessage msg, CancellationToken token = default)
        {
            if (!_messageResponse.TryGetValue(msg.GetType(), out var result))
            {
                throw new ButtplugMessageException($"No response registered for {msg.GetType().Name}.");
            }

            result.Id = msg.Id;
            return Task.FromResult(result);
        }
    }
}
