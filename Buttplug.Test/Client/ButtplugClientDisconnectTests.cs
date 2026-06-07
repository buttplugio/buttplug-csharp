// <copyright file="ButtplugClientDisconnectTests.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;
using FluentAssertions;
using NUnit.Framework;

namespace Buttplug.Client.Test
{
    [TestFixture]
    public class ButtplugClientDisconnectTests
    {
        [Test]
        public async Task TestDisconnectAsyncOnlyRaisesServerDisconnectOnce()
        {
            var client = new ButtplugClient("Test Client");
            var connector = new DisconnectEventConnector();
            var disconnectCount = 0;
            client.ServerDisconnect += (sender, args) => disconnectCount++;

            await client.ConnectAsync(connector);
            await client.DisconnectAsync();

            disconnectCount.Should().Be(1);
        }

        private class DisconnectEventConnector : IButtplugClientConnector
        {
            public event EventHandler<MessageReceivedEventArgs> MessageReceived;

            public event EventHandler<ButtplugExceptionEventArgs> InvalidMessageReceived;

            public event EventHandler Disconnected;

            public bool Connected { get; private set; }

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
                ButtplugMessage response;
                switch (msg)
                {
                    case RequestServerInfo _:
                        response = new ServerInfo("Test Server", ButtplugConsts.ProtocolVersionMajor, ButtplugConsts.ProtocolVersionMinor, 0, msg.Id);
                        break;
                    case RequestDeviceList _:
                        response = new DeviceList(new Dictionary<uint, DeviceInfo>(), msg.Id);
                        break;
                    default:
                        response = new Ok(msg.Id);
                        break;
                }

                return Task.FromResult(response);
            }

            protected virtual void OnMessageReceived(MessageReceivedEventArgs e)
            {
                MessageReceived?.Invoke(this, e);
            }

            protected virtual void OnInvalidMessageReceived(ButtplugExceptionEventArgs e)
            {
                InvalidMessageReceived?.Invoke(this, e);
            }
        }
    }
}
