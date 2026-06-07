// <copyright file="ButtplugConnectorMessageSorterTests.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;
using FluentAssertions;
using NUnit.Framework;

namespace Buttplug.Client.Test
{
    [TestFixture]
    public class ButtplugConnectorMessageSorterTests
    {
        [Test]
        public async Task TestPrepareMessageCancelsAndRemovesWaitingMessage()
        {
            var sorter = new ButtplugConnectorMessageSorter();
            var cancellation = new CancellationTokenSource();
            var msg = new Ping();

            var responseTask = sorter.PrepareMessage(msg, cancellation.Token);
            cancellation.Cancel();

            Func<Task> awaitResponse = async () => await responseTask;
            await awaitResponse.Should().ThrowAsync<TaskCanceledException>();
            sorter
                .Invoking(x => x.CheckMessage(new Ok(msg.Id)))
                .Should()
                .Throw<ButtplugMessageException>();
        }
    }
}
