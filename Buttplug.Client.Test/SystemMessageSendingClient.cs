// <copyright file="ButtplugClient.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

// Test file, disable ConfigureAwait checking.
// ReSharper disable ConsiderUsingConfigureAwait

using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace Buttplug.Client.Test
{
    public class SystemMessageSendingClient : ButtplugClient
    {
        public SystemMessageSendingClient([NotNull] string aClientName, [NotNull] IButtplugClientConnector aConnector)
            : base(aClientName, aConnector)
        {
        }

        public async Task SendSystemIdMessage()
        {
            await SendMessageAsync(new StartScanning(ButtplugConsts.SystemMsgId));
        }

        public async Task SendOutgoingOnlyMessage()
        {
            await SendMessageAsync(new Ok(ButtplugConsts.DefaultMsgId));
        }
    }
}