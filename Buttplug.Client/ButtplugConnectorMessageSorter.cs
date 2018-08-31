// <copyright file="ButtplugConnectorMessageSorter.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace Buttplug.Client
{
    public class ButtplugConnectorMessageSorter
    {
        /// <summary>
        /// Holds count for message IDs, if needed.
        /// </summary>
        private int _counter = 0;

        /// <summary>
        /// Gets the next available message ID. In most cases, setting the message ID is done automatically.
        /// </summary>
        public uint NextMsgId => Convert.ToUInt32(Interlocked.Increment(ref _counter));

        /// <summary>
        /// Stores messages waiting for reply from the server.
        /// </summary>
        [NotNull]
        private readonly ConcurrentDictionary<uint, TaskCompletionSource<ButtplugMessage>> _waitingMsgs =
            new ConcurrentDictionary<uint, TaskCompletionSource<ButtplugMessage>>();

        /// <summary>
        /// Used for dispatching events to the owning application context.
        /// </summary>
        [NotNull]
        private readonly SynchronizationContext _owningDispatcher = SynchronizationContext.Current ?? new SynchronizationContext();

        public Task<ButtplugMessage> PrepareMessage(ButtplugMessage aMsg)
        {
            // The client always increments the IDs on outgoing messages
            aMsg.Id = NextMsgId;

            var promise = new TaskCompletionSource<ButtplugMessage>();
            _waitingMsgs.TryAdd(aMsg.Id, promise);

            return promise.Task;
        }

        public bool CheckMessage(ButtplugMessage aMsg)
        {
            // We'll never match a system message, those are server -> client only.
            if (aMsg.Id == 0)
            {
                return false;
            }

            // If we haven't gotten a system message and we're not currently waiting for the message
            // id, return.
            if (!_waitingMsgs.TryRemove(aMsg.Id, out var queued))
            {
                // TODO In what situation would we receive something we're not listening for? Should this error?
                return false;
            }

            queued.TrySetResult(aMsg);
            return true;
        }
    }
}