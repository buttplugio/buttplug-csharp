// <copyright file="ButtplugClientIPCConnector.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Messages;

namespace Buttplug.Client.Connectors
{
    public class ButtplugClientIPCConnector : ButtplugRemoteJSONConnector, IButtplugClientConnector
    {
        public event EventHandler Disconnected;

        /// <summary>
        /// Used for dispatching events to the owning application context.
        /// </summary>
        private readonly SynchronizationContext _owningDispatcher = SynchronizationContext.Current ?? new SynchronizationContext();

        private readonly string _ipcSocketName;

        private NamedPipeClientStream _pipeClient;

        private Task _readTask;

        /// <summary>
        /// Status of the client connection.
        /// </summary>
        /// <returns>True if client is currently connected.</returns>
        public bool Connected => _pipeClient?.IsConnected ?? false;

        /// <summary>
        /// </summary>
        /// <param name="iPCSocketName">
        /// Name of the IPC Socket to use. Defaults to "ButtplugPipe".
        /// </param>
        public ButtplugClientIPCConnector(string iPCSocketName = "ButtplugPipe")
        {
            _ipcSocketName = iPCSocketName;
        }

        public async Task ConnectAsync(CancellationToken token = default)
        {
            if (Connected)
            {
                throw new InvalidOperationException("Already connected!");
            }

            _pipeClient = new NamedPipeClientStream(".",
                _ipcSocketName,
                PipeDirection.InOut, PipeOptions.Asynchronous,
                TokenImpersonationLevel.Impersonation);

            await _pipeClient.ConnectAsync(token).ConfigureAwait(false);

            _readTask = new Task(async () => await PipeReader(token).ConfigureAwait(false),
                token,
                TaskCreationOptions.LongRunning);
            _readTask.Start();
        }

        public async Task DisconnectAsync(CancellationToken token = default)
        {
            // TODO Create internal token for cancellation and use link source with external key
            //_cancellationToken.Cancel();
            _pipeClient.Close();
            await _readTask.ConfigureAwait(false);
        }

        public async Task<ButtplugMessage> SendAsync(ButtplugMessage msg, CancellationToken token = default)
        {
            var (msgString, promise) = PrepareMessage(msg);
            var output = Encoding.UTF8.GetBytes(msgString);

            if (Connected)
            {
                _pipeClient.Write(output, 0, output.Length);
            }
            else
            {
                throw new ButtplugClientConnectorException("Bad Pipe state!");
            }

            return await promise.ConfigureAwait(false);
        }

        private async Task PipeReader(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _pipeClient?.IsConnected == true)
            {
                var buffer = new byte[4096];
                var msg = string.Empty;
                var len = -1;
                while (len < 0 || (len == buffer.Length && buffer[4095] != '\0'))
                {
                    try
                    {
                        len = await _pipeClient.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                    }
                    catch
                    {
                        if (!Connected)
                        {
                            _owningDispatcher.Send(_ => Disconnected?.Invoke(this, EventArgs.Empty), null);
                            return;
                        }

                        continue;
                    }

                    if (len > 0)
                    {
                        msg += Encoding.UTF8.GetString(buffer, 0, len);
                    }
                }

                ReceiveMessages(msg);
            }

            // If we somehow still have some live messages, throw exceptions so they aren't stuck.
            _owningDispatcher.Send(_ => Dispose(), null);
            _owningDispatcher.Send(_ => Disconnected?.Invoke(this, EventArgs.Empty), null);
        }
    }
}