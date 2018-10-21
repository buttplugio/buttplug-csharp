// <copyright file="ButtplugClientTestConnector.cs" company="Nonpolynomial Labs LLC">
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
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using NUnit.Framework;

namespace Buttplug.Client.Test
{
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Test classes can skip documentation requirements")]
    public class ButtplugClientTestConnector : IButtplugClientConnector
    {
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public event EventHandler<ButtplugExceptionEventArgs> InvalidMessageReceived;

        public event EventHandler Disconnected;

        public IButtplugLogManager LogManager { private get; set; }

        public bool Connected { get; private set; }

        private Dictionary<Type, ButtplugMessage> _messageResponse;

        public ButtplugClientTestConnector()
        {
            _messageResponse = new Dictionary<Type, ButtplugMessage>
            {
                { typeof(RequestServerInfo), new ServerInfo("Test Server", ButtplugConsts.CurrentSpecVersion, 0) },
                { typeof(RequestDeviceList), new DeviceList(new DeviceMessageInfo[0], ButtplugConsts.DefaultMsgId) },
            };
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
            Connected = true;
            return Task.CompletedTask;
        }

        public Task DisconnectAsync(CancellationToken aToken = default(CancellationToken))
        {
            Connected = false;
            return Task.CompletedTask;
        }

        public Task<ButtplugMessage> SendAsync(ButtplugMessage aMsg, CancellationToken aToken = default(CancellationToken))
        {
            // This will just throw if we don't have a corresponding reply to the message, which is fine.
            var msg = _messageResponse[aMsg.GetType()];

            msg.Id = aMsg.Id;
            return Task.FromResult(msg);
        }
    }
}