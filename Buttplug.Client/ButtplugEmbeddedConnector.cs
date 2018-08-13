// <copyright file="ButtplugEmbeddedConnector.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Server;

namespace Buttplug.Client
{
    public class ButtplugEmbeddedConnector : IButtplugClientConnector
    {
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

#pragma warning disable CS0067 // Unused event
        /// <inheritdoc />
        public event EventHandler Disconnected;
#pragma warning restore CS0067 // Unused event

        public bool Connected { get; private set; }

        public readonly ButtplugServer Server;

        public ButtplugEmbeddedConnector(string aServerName, uint aMaxPingTime = 0, DeviceManager aDeviceManager = null)
        {
            Server = new ButtplugServer(aServerName, aMaxPingTime, aDeviceManager);
            Server.MessageReceived += OnServerMessageReceived;
        }

        public ButtplugEmbeddedConnector(ButtplugServer aServer)
        {
            Server = aServer;
            Server.MessageReceived += OnServerMessageReceived;
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

        public async Task<ButtplugMessage> SendAsync(ButtplugMessage aMsg, CancellationToken aToken = default(CancellationToken))
        {
            return await Server.SendMessage(aMsg);
        }

        private void OnServerMessageReceived(object e, MessageReceivedEventArgs aMsgEvent)
        {
            MessageReceived?.Invoke(this, aMsgEvent);
        }
    }
}
