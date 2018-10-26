using System;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace Buttplug.Client.Connectors.IPCConnector
{
    public class ButtplugClientIPCConnector : ButtplugRemoteJSONConnector, IButtplugClientConnector
    {
        public event EventHandler Disconnected;

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        [NotNull] private readonly object _sendLock = new object();

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
        /// <param name="aIPCSocketName">
        /// Name of the IPC Socket to use. Defaults to "ButtplugPipe".
        /// </param>
        public ButtplugClientIPCConnector(string aIPCSocketName = "ButtplugPipe")
        {
            _ipcSocketName = aIPCSocketName;
        }

        public async Task ConnectAsync(CancellationToken aToken = default(CancellationToken))
        {
            if (Connected)
            {
                throw new InvalidOperationException("Already connected!");
            }

            _pipeClient = new NamedPipeClientStream(".",
                _ipcSocketName,
                PipeDirection.InOut, PipeOptions.Asynchronous,
                TokenImpersonationLevel.Impersonation);

            await _pipeClient.ConnectAsync(aToken).ConfigureAwait(false);

            _readTask = new Task(async () => { await pipeReader(aToken).ConfigureAwait(false); },
                aToken,
                TaskCreationOptions.LongRunning);
            _readTask.Start();
        }

        public async Task DisconnectAsync(CancellationToken aToken = default(CancellationToken))
        {
            // TODO Create internal token for cancellation and use link source with external key
            //_cancellationToken.Cancel();
            _pipeClient.Close();
            await _readTask.ConfigureAwait(false);
        }

        public async Task<ButtplugMessage> SendAsync(ButtplugMessage aMsg, CancellationToken aToken = default(CancellationToken))
        {
            var (msgString, promise) = PrepareMessage(aMsg);
            var output = Encoding.UTF8.GetBytes(msgString);

                lock (_sendLock)
                {
                    if (Connected)
                    {
                        _pipeClient.Write(output, 0, output.Length);
                    }
                    else
                    {
                        throw new ButtplugClientConnectorException("Bad Pipe state!");
                    }
                }

                return await promise.ConfigureAwait(false);
        }

        private async Task pipeReader(CancellationToken aCancellationToken)
        {
            while (!aCancellationToken.IsCancellationRequested && _pipeClient != null && _pipeClient.IsConnected)
            {
                var buffer = new byte[4096];
                var msg = string.Empty;
                var len = -1;
                while (len < 0 || (len == buffer.Length && buffer[4095] != '\0'))
                {
                    try
                    {
                        len = await _pipeClient.ReadAsync(buffer, 0, buffer.Length, aCancellationToken).ConfigureAwait(false);
                    }
                    catch
                    {
                        if (!Connected)
                        {
                            _owningDispatcher.Send(_ => Disconnected?.Invoke(this, new EventArgs()), null);
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
            _owningDispatcher.Send(_ => Disconnected?.Invoke(this, new EventArgs()), null);
        }
    }
}