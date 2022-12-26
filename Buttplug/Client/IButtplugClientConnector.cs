// <copyright file="IButtplugClientConnector.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;

using Buttplug.Core.Messages;

namespace Buttplug.Client
{
    public interface IButtplugClientConnector
    {
        event EventHandler<MessageReceivedEventArgs> MessageReceived;

        event EventHandler<ButtplugExceptionEventArgs> InvalidMessageReceived;

        event EventHandler Disconnected;

        Task ConnectAsync(CancellationToken token = default(CancellationToken));

        Task DisconnectAsync(CancellationToken token = default(CancellationToken));

        Task<ButtplugMessage> SendAsync(ButtplugMessage msg, CancellationToken token = default(CancellationToken));

        bool Connected { get; }
    }
}