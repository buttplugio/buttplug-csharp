// <copyright file="ButtplugEmbeddedConnector.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using Buttplug.Server;
using JetBrains.Annotations;

namespace Buttplug.Client
{
    // ReSharper disable once UnusedMember.Global
    public class ButtplugEmbeddedConnector : IButtplugClientConnector
    {
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public event EventHandler<ButtplugExceptionEventArgs> InvalidMessageReceived;

        public event EventHandler Disconnected;

        public bool Connected { get; private set; }

        public readonly ButtplugServer Server;

        public IButtplugLogManager LogManager
        {
            set
            {
                _logManager = value;
                _logger = _logManager.GetLogger(GetType());
            }
        }

        [CanBeNull]
        private IButtplugLogManager _logManager;

        [CanBeNull]
        private IButtplugLog _logger;

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
            return await Server.SendMessageAsync(aMsg).ConfigureAwait(false);
        }

        private void OnServerMessageReceived(object e, MessageReceivedEventArgs aMsgEvent)
        {
            MessageReceived?.Invoke(this, aMsgEvent);
        }
    }
}
