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

namespace Buttplug.Client
{
    public class ButtplugConnectorMessageSorter : IDisposable
    {
        /// <summary>
        /// Holds count for message IDs, if needed.
        /// </summary>
        private int _counter;

        /// <summary>
        /// Gets the next available message ID. In most cases, setting the message ID is done automatically.
        /// </summary>
        public uint NextMsgId => Convert.ToUInt32(Interlocked.Increment(ref _counter));

        /// <summary>
        /// Stores messages waiting for reply from the server.
        /// </summary>
        private readonly ConcurrentDictionary<uint, WaitingMessage> _waitingMsgs =
            new ConcurrentDictionary<uint, WaitingMessage>();

        public Task<ButtplugMessage> PrepareMessage(ButtplugMessage msg, CancellationToken token = default)
        {
            // The client always increments the IDs on outgoing messages
            msg.Id = NextMsgId;

            var waitingMessage = new WaitingMessage(new TaskCompletionSource<ButtplugMessage>());
            _waitingMsgs.TryAdd(msg.Id, waitingMessage);

            if (token.CanBeCanceled)
            {
                waitingMessage.Cancellation = token.Register(() =>
                {
                    if (_waitingMsgs.TryRemove(msg.Id, out var queued))
                    {
                        queued.Promise.TrySetCanceled();
                    }
                });
            }

            return waitingMessage.Promise.Task;
        }

        public void CheckMessage(ButtplugMessage msg)
        {
            // We'll never match a system message, those are server -> client only.
            if (msg.Id == 0)
            {
                throw new ButtplugMessageException("Cannot sort message with System ID", msg.Id);
            }

            // If we haven't gotten a system message and we're not currently waiting for the message
            // id, throw.
            if (!_waitingMsgs.TryRemove(msg.Id, out var queued))
            {
                throw new ButtplugMessageException("Message with non-matching ID received.", msg.Id);
            }

            queued.Dispose();

            if (msg is Error errMsg)
            {
                queued.Promise.SetException(ButtplugException.FromError(errMsg));
                return;
            }

            queued.Promise.SetResult(msg);
        }

        protected virtual void Dispose(bool disposing)
        {
            // If we've somehow destructed while holding tasks, throw exceptions at all of them.
            foreach (var pair in _waitingMsgs)
            {
                if (_waitingMsgs.TryRemove(pair.Key, out var queued))
                {
                    queued.Dispose();
                    queued.Promise.TrySetException(new Exception("Sorter has been destroyed with live tasks still in queue."));
                }
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private class WaitingMessage : IDisposable
        {
            public WaitingMessage(TaskCompletionSource<ButtplugMessage> promise)
            {
                Promise = promise;
            }

            public TaskCompletionSource<ButtplugMessage> Promise { get; }

            public CancellationTokenRegistration Cancellation { get; set; }

            public void Dispose()
            {
                Cancellation.Dispose();
            }
        }
    }
}
