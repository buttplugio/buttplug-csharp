using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO.Pipes;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;
using JetBrains.Annotations;
using WebSocket4Net;
using static Buttplug.Client.DeviceEventArgs;

namespace Buttplug.Client
{
    /// <summary>
    /// This is the Buttplug C# WebSocket Client implementation.
    /// It's intended to abstract away the process of communicating with
    /// a Buttplug Server over Named Pipes, so you don't have to worry
    /// about the lower level protocol.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class ButtplugIPCClient : ButtplugAbsractClient
    {
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable

        [NotNull] private readonly object _sendLock = new object();

        private NamedPipeClientStream _pipeClient;

        private Task _readThread;

        private ConcurrentQueue<ButtplugMessage> _msgQueue = new ConcurrentQueue<ButtplugMessage>();

        /// <summary>
        /// Event fired on Buttplug device added
        /// Should fire only immediately after connect or whilst scanning for devices
        /// </summary>
        [CanBeNull]
        public override event EventHandler<DeviceEventArgs> DeviceAdded;

        /// <summary>
        /// Event fired on Buttplug device removed
        /// Can fire are any time
        /// </summary>
        [CanBeNull]
        public override event EventHandler<DeviceEventArgs> DeviceRemoved;

        /// <summary>
        /// Event fired when the server has stopped scanning for devices
        /// </summary>
        [CanBeNull]
        public override event EventHandler<ScanningFinishedEventArgs> ScanningFinished;

        /// <summary>
        /// Event fired when an error has been encountered
        /// This may be internal client exceptions or Error messages from the server
        /// </summary>
        [CanBeNull]
        public override event EventHandler<ErrorEventArgs> ErrorReceived;

        /// <summary>
        /// Event fired when the client recieves a Log message
        /// Should only fire if the client requests logs
        /// </summary>
        [CanBeNull]
        public override event EventHandler<LogEventArgs> Log;

        /// <summary>
        /// Used for error handling during the connection process.
        /// </summary>
        private bool _connecting;

        /// <summary>
        /// Status of the client connection.
        /// </summary>
        /// <returns>True if client is currently connected.</returns>
        public override bool IsConnected => _pipeClient?.IsConnected ?? false;

        /// <summary>
        /// Initializes a new instance of the <see cref="ButtplugIPCClient"/> class.
        /// </summary>
        /// <param name="aClientName">The name of the client to present to the server</param>
        public ButtplugIPCClient(string aClientName)
            : base(aClientName)
        {
        }

        /// <summary>
        /// Creates the connection to the Buttplug Server and performs the protocol hanshake.
        /// Once the WebSocket connetion is open, the RequestServerInfo message is sent;
        /// the response is used to set up the ping timer loop. The RequestDeviceList
        /// message is also sent here so that any devices the server is already connected to
        /// are made known to the client.
        ///
        /// <b>Important:</b> Ensure that <see cref="DeviceAdded"/>, <see cref="DeviceRemoved"/>
        /// and <see cref="ErrorReceived"/> handlers are set before Connect is called.
        /// </summary>
        /// <param name="aURL">The URL for the Buttplug WebSocket Server. This will likely be in the form wss://localhost:12345 (wss:// is to ws:// as https:// is to http://)</param>
        /// <param name="aIgnoreSSLErrors">When using SSL (wss://), this option prevents bad certificates from causing connection failures</param>
        /// <returns>An untyped Task; the await/async equivelent of void</returns>
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Embedded acronyms")]
        public override async Task Connect(Uri aURL, bool aIgnoreSSLErrors = false)
        {
            if (IsConnected)
            {
                throw new InvalidOperationException("Already connected!");
            }

            _pipeClient = new NamedPipeClientStream(".",
                aURL.Host,
                PipeDirection.InOut, PipeOptions.Asynchronous,
                TokenImpersonationLevel.Impersonation);
            _waitingMsgs.Clear();
            _devices.Clear();
            _counter = 1;

            _connecting = true;
            _pipeClient.Connect(2000);
            _connecting = false;

            _tokenSource = new CancellationTokenSource();
            _readThread = new Task(() => { pipeReader(_tokenSource.Token); }, _tokenSource.Token,
                TaskCreationOptions.LongRunning);
            _readThread.Start();

            var res = await SendMessage(new RequestServerInfo(_clientName));
            switch (res)
            {
                case ServerInfo si:
                    if (si.MaxPingTime > 0)
                    {
                        _pingTimer = new Timer(OnPingTimer, null, 0,
                            Convert.ToInt32(Math.Round(((double)si.MaxPingTime) / 2, 0)));
                    }

                    if (si.MessageVersion < ButtplugMessage.CurrentSchemaVersion)
                    {
                        throw new Exception("Buttplug Server's schema version (" + si.MessageVersion +
                                            ") is less than the client's (" + ButtplugMessage.CurrentSchemaVersion +
                                            "). A newer server is required.");
                    }

                    // Get full device list and populate internal list
                    var resp = await SendMessage(new RequestDeviceList());
                    if ((resp as DeviceList)?.Devices == null)
                    {
                        if (resp is Error)
                        {
                            _owningDispatcher.Send(
                                _ => { ErrorReceived?.Invoke(this, new ErrorEventArgs(resp as Error)); }, null);
                        }

                        return;
                    }

                    foreach (var d in (resp as DeviceList).Devices)
                    {
                        if (_devices.ContainsKey(d.DeviceIndex))
                        {
                            continue;
                        }

                        var device = new ButtplugClientDevice(d);
                        if (_devices.TryAdd(d.DeviceIndex, device))
                        {
                            _owningDispatcher.Send(
                                _ => { DeviceAdded?.Invoke(this, new DeviceEventArgs(device, DeviceAction.ADDED)); },
                                null);
                        }
                    }

                    break;

                case Error e:
                    throw new Exception(e.ErrorMessage);

                default:
                    throw new Exception("Unexpected message returned: " + res.GetType());
            }
        }

        /// <summary>
        /// Closes the WebSocket Connection.
        /// </summary>
        /// <returns>An untyped Task; the await/async equivelent of void</returns>
        public override async Task Disconnect()
        {
            if (_pingTimer != null)
            {
                _pingTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _pingTimer = null;
            }

            try
            {
                while (IsConnected)
                {
                    _pipeClient.Close();
                }
            }
            catch
            {
                // noop - something when wrong closing the socket, but we're
                // about to dispose of it anyway.
            }

            try
            {
                _tokenSource.Cancel();
            }
            catch
            {
                // noop - something when wrong closing the socket, but we're
                // about to dispose of it anyway.
            }

            _pipeClient = null;

            var max = 3;
            while (max-- > 0 && _waitingMsgs.Count != 0)
            {
                foreach (var msgId in _waitingMsgs.Keys)
                {
                    if (_waitingMsgs.TryRemove(msgId, out TaskCompletionSource<ButtplugMessage> promise))
                    {
                        promise.SetResult(new Error("Connection closed!", Error.ErrorClass.ERROR_UNKNOWN,
                            ButtplugConsts.SystemMsgId));
                    }
                }
            }

            _counter = 1;
        }

        /// <summary>
        /// Sends a message and returns the resulting message
        /// </summary>
        /// <param name="aMsg">Message to send.</param>
        /// <returns>The response <see cref="ButtplugMessage"/></returns>
        protected override async Task<ButtplugMessage> SendMessage(ButtplugMessage aMsg)
        {
            // The client always increments the IDs on outgoing messages
            aMsg.Id = NextMsgId;

            var promise = new TaskCompletionSource<ButtplugMessage>();
            _waitingMsgs.TryAdd(aMsg.Id, promise);

            var output = Encoding.ASCII.GetBytes(Serialize(aMsg));

            try
            {
                lock (_sendLock)
                {
                    if (IsConnected)
                    {
                        _pipeClient.Write(output, 0, output.Length);
                    }
                    else
                    {
                        return new Error("Bad WS state!", Error.ErrorClass.ERROR_UNKNOWN, ButtplugConsts.SystemMsgId);
                    }
                }

                return await promise.Task;
            }
            catch (Exception e)
            {
                // Noop - WS probably closed on us during read
                return new Error(e.Message, Error.ErrorClass.ERROR_UNKNOWN, ButtplugConsts.SystemMsgId);
            }
        }

        private void pipeReader(CancellationToken aCancellationToken)
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
                        var waiter = _pipeClient.ReadAsync(buffer, 0, buffer.Length, aCancellationToken);
                        while (!waiter.GetAwaiter().IsCompleted)
                        {
                            if (!_pipeClient.IsConnected)
                            {
                                return;
                            }

                            Thread.Sleep(10);
                        }

                        len = waiter.GetAwaiter().GetResult();
                    }
                    catch
                    {
                        continue;
                    }

                    if (len > 0)
                    {
                        msg += Encoding.ASCII.GetString(buffer, 0, len);
                    }
                }

                Console.Out.WriteLine(msg);
                var parsed = _parser.Deserialize(msg);

                MessageReceivedHandler(parsed);
            }

            if (IsConnected)
            {
                Disconnect().Wait();
            }
        }
    }
}
