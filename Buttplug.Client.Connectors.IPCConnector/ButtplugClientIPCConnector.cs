using Buttplug.Core;
using System;
using System.Collections.Concurrent;
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

        private Task _readThread;

        private CancellationTokenSource _cancellationToken;

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

        /// <summary>
        /// Creates the connection to the Buttplug Server and performs the protocol handshake.
        /// </summary>
        /// <returns>Nothing (Task used for async/await)</returns>
        public async Task Connect()
        {
            if (Connected)
            {
                throw new InvalidOperationException("Already connected!");
            }

            _pipeClient = new NamedPipeClientStream(".",
                _ipcSocketName,
                PipeDirection.InOut, PipeOptions.Asynchronous,
                TokenImpersonationLevel.Impersonation);

            await _pipeClient.ConnectAsync(2000);

            _cancellationToken = new CancellationTokenSource();
            _readThread = new Task(async () => { await pipeReader(_cancellationToken.Token); },
                _cancellationToken.Token,
                TaskCreationOptions.LongRunning);
            _readThread.Start();
        }

        /// <summary>
        /// Closes the WebSocket Connection.
        /// </summary>
        /// <returns>Nothing (Task used for async/await)</returns>
        public async Task Disconnect()
        {
            _cancellationToken.Cancel();
            _pipeClient.Close();
            _readThread.Wait();
            _owningDispatcher.Send(_ => Disconnected?.Invoke(this, new EventArgs()), null);
        }

        public async Task<ButtplugMessage> Send(ButtplugMessage aMsg)
        {
            var (msgString, promise) = PrepareMessage(aMsg);
            var output = Encoding.UTF8.GetBytes(msgString);
            try
            {
                lock (_sendLock)
                {
                    if (Connected)
                    {
                        _pipeClient.Write(output, 0, output.Length);
                    }
                    else
                    {
                        return new Error("Bad Pipe state!", Error.ErrorClass.ERROR_UNKNOWN, ButtplugConsts.SystemMsgId);
                    }
                }

                return await promise;
            }
            catch (Exception e)
            {
                // Noop - WS probably closed on us during read
                return new Error(e.Message, Error.ErrorClass.ERROR_UNKNOWN, ButtplugConsts.SystemMsgId);
            }
        }

        private async Task pipeReader(CancellationToken aCancellationToken)
        {
            while (!aCancellationToken.IsCancellationRequested && _pipeClient != null && _pipeClient.IsConnected)
            {
                var buffer = new byte[4096];
                string msg = string.Empty;
                var len = -1;
                while (len < 0 || (len == buffer.Length && buffer[4095] != '\0'))
                {
                    try
                    {
                        len = await _pipeClient.ReadAsync(buffer, 0, buffer.Length, aCancellationToken);

                        // TODO Why do we need this sleep? Shouldn't this block until we receive data?
                        Thread.Sleep(10);
                    }
                    catch
                    {
                        if (!Connected)
                        {
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
        }
    }
}