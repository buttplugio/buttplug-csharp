using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
    /// It's intended to abstract away the process of comunicating with
    /// a Buttplug Server over WebSockets, so you don't have to worry
    /// about the lower level protocol.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class ButtplugWSClient
    {
        [NotNull]
        private readonly ButtplugJsonMessageParser _parser;

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        [NotNull]
        private readonly IButtplugLog _bpLogger;

        [NotNull]
        private readonly object _sendLock = new object();

        [NotNull]
        private readonly string _clientName;

        [NotNull]
        private readonly CancellationTokenSource _tokenSource;

        [NotNull]
        private readonly SynchronizationContext _owningDispatcher;

        [NotNull]
        private readonly ConcurrentDictionary<uint, TaskCompletionSource<ButtplugMessage>> _waitingMsgs =
            new ConcurrentDictionary<uint, TaskCompletionSource<ButtplugMessage>>();

        [NotNull]
        private readonly ConcurrentDictionary<uint, ButtplugClientDevice> _devices =
            new ConcurrentDictionary<uint, ButtplugClientDevice>();

        [CanBeNull]
        private WebSocket _ws;

        [CanBeNull]
        private Timer _pingTimer;

        private uint _messageSchemaVersion;

        private int _counter;

        /// <summary>
        /// Event fired on Buttplug device added
        /// Should fire only immediatly after connect or whilst scanning for devices
        /// </summary>
        [CanBeNull]
        public event EventHandler<DeviceEventArgs> DeviceAdded;

        /// <summary>
        /// Event fired on Buttplug device removed
        /// Can fire are any time
        /// </summary>
        [CanBeNull]
        public event EventHandler<DeviceEventArgs> DeviceRemoved;

        /// <summary>
        /// Event fired when the server has stopped scanning for devices
        /// </summary>
        [CanBeNull]
        public event EventHandler<ScanningFinishedEventArgs> ScanningFinished;

        /// <summary>
        /// Event fired when an error has been encountered
        /// This may be internal client exceptions or Error messages from the server
        /// </summary>
        [CanBeNull]
        public event EventHandler<ErrorEventArgs> ErrorReceived;

        /// <summary>
        /// Event fired when the client recieves a Log message
        /// Should only fire if the client requests logs
        /// </summary>
        [CanBeNull]
        public event EventHandler<LogEventArgs> Log;

        private TaskCompletionSource<bool> _connectedOrFailed;

        private TaskCompletionSource<bool> _disconnected;

        private bool _connecting;

        /// <summary>
        /// Gets the next available message ID
        /// In most cases setting the message ID is done automatically
        /// </summary>
        public uint NextMsgId => Convert.ToUInt32(Interlocked.Increment(ref _counter));

        /// <summary>
        /// Gets the connected Buttplug devices
        /// </summary>
        public ButtplugClientDevice[] Devices => _devices.Values.ToArray();

        /// <summary>
        /// Is the client connected?
        /// </summary>
        public bool IsConnected => _ws != null &&
                                   (_ws.State == WebSocketState.Connecting ||
                                    _ws.State == WebSocketState.Open);

        /// <summary>
        /// Initializes a new instance of the <see cref="ButtplugWSClient"/> class.
        /// </summary>
        /// <param name="aClientName">The name of the client to present to the server</param>
        public ButtplugWSClient(string aClientName)
        {
            _clientName = aClientName;
            IButtplugLogManager bpLogManager = new ButtplugLogManager();
            _bpLogger = bpLogManager.GetLogger(GetType());
            _parser = new ButtplugJsonMessageParser(bpLogManager);
            _bpLogger.Info("Finished setting up ButtplugClient");
            _owningDispatcher = SynchronizationContext.Current ?? new SynchronizationContext();
            _tokenSource = new CancellationTokenSource();
            _counter = 0;
            _messageSchemaVersion = ButtplugMessage.CurrentSchemaVersion;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="ButtplugWSClient"/> class.
        /// The destrctor will close the connection, if it's still open.
        /// </summary>
        ~ButtplugWSClient()
        {
            Disconnect().Wait();
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
        public async Task Connect(Uri aURL, bool aIgnoreSSLErrors = false)
        {
            if (_ws != null && (_ws.State == WebSocketState.Connecting || _ws.State == WebSocketState.Open))
            {
                throw new InvalidOperationException("Already connected!");
            }

            _ws = new WebSocket(aURL.ToString());
            _waitingMsgs.Clear();
            _devices.Clear();
            _counter = 1;

            _connectedOrFailed = new TaskCompletionSource<bool>();
            _disconnected = new TaskCompletionSource<bool>();

            _ws.Opened += OpenedHandler;
            _ws.Closed += ClosedHandler;
            _ws.Error += ErrorHandler;

            if (aIgnoreSSLErrors)
            {
                _ws.Security.AllowNameMismatchCertificate = true;
                _ws.Security.AllowUnstrustedCertificate = true;
                _ws.Security.AllowCertificateChainErrors = true;
            }

            _ws.Open();

            _connecting = true;
            await _connectedOrFailed.Task;
            _connecting = false;

            if (_ws.State != WebSocketState.Open)
            {
                throw new Exception("Connection failed!");
            }

            _ws.MessageReceived += MessageReceivedHandler;

            var res = await SendMessage(new RequestServerInfo(_clientName));
            switch (res)
            {
                case ServerInfo si:
                    if (si.MaxPingTime > 0)
                    {
                        _pingTimer = new Timer(OnPingTimer, null, 0, Convert.ToInt32(Math.Round(((double)si.MaxPingTime) / 2, 0)));
                    }

                    _messageSchemaVersion = si.MessageVersion;
                    if (_messageSchemaVersion < ButtplugMessage.CurrentSchemaVersion)
                    {
                        throw new Exception("Buttplug Server's schema version (" + _messageSchemaVersion +
                            ") is less than the client's (" + ButtplugMessage.CurrentSchemaVersion +
                            "). A newer server is required.");
                    }

                    // Get full device list and populate internal list
                    var resp = await SendMessage(new RequestDeviceList());
                    if ((resp as DeviceList)?.Devices == null)
                    {
                        if (resp is Error)
                        {
                            _owningDispatcher.Send(_ =>
                            {
                                ErrorReceived?.Invoke(this, new ErrorEventArgs(resp as Error));
                            }, null);
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
                            _owningDispatcher.Send(_ =>
                            {
                                DeviceAdded?.Invoke(this, new DeviceEventArgs(device, DeviceAction.ADDED));
                            }, null);
                        }
                    }

                    break;

                case Error e:
                    throw new Exception(e.ErrorMessage);

                default:
                    throw new Exception("Unexpecte message returned: " + res.GetType());
            }
        }

        private void ErrorHandler(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            if (_connecting)
            {
                _connectedOrFailed.TrySetResult(false);
            }

            _owningDispatcher.Send(_ =>
            {
                ErrorReceived?.Invoke(this, new ErrorEventArgs(e.Exception));

                if (_ws?.State != WebSocketState.Open)
                {
                    Disconnect().Wait();
                }
            }, null);
        }

        private void ClosedHandler(object sender, EventArgs e)
        {
            _disconnected.TrySetResult(true);
            _owningDispatcher.Send(_ =>
            {
                Disconnect().Wait();
            }, null);
        }

        private void OpenedHandler(object sender, EventArgs e)
        {
            _connectedOrFailed.TrySetResult(true);
        }

        /// <summary>
        /// Closes the WebSocket Connection.
        /// </summary>
        /// <returns>An untyped Task; the await/async equivelent of void</returns>
        public async Task Disconnect()
        {
            if (_pingTimer != null)
            {
                _pingTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _pingTimer = null;
            }

            try
            {
                while (_ws != null && _ws.State != WebSocketState.Closed)
                {
                    if (_ws.State == WebSocketState.Closing || _ws.State == WebSocketState.Closed)
                    {
                        continue;
                    }

                    _ws.Close("Client shutdown");
                    await _disconnected.Task;
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

            _ws = null;

            var max = 3;
            while (max-- > 0 && _waitingMsgs.Count != 0)
            {
                foreach (var msgId in _waitingMsgs.Keys)
                {
                    if (_waitingMsgs.TryRemove(msgId, out TaskCompletionSource<ButtplugMessage> promise))
                    {
                        promise.SetResult(new Error("Connection closed!", Error.ErrorClass.ERROR_UNKNOWN, ButtplugConsts.SystemMsgId));
                    }
                }
            }

            _counter = 1;
        }

        private void MessageReceivedHandler(object aSender, WebSocket4Net.MessageReceivedEventArgs aArgs)
        {
            var msgs = Deserialize(aArgs.Message);
            foreach (var msg in msgs)
            {
                if (msg.Id > 0 && _waitingMsgs.TryRemove(msg.Id, out TaskCompletionSource<ButtplugMessage> queued))
                {
                    queued.TrySetResult(msg);
                    continue;
                }

                switch (msg)
                {
                    case Log l:
                        _owningDispatcher.Send(_ =>
                        {
                            Log?.Invoke(this, new LogEventArgs(l));
                        }, null);
                        break;

                    case DeviceAdded d:
                        var dev = new ButtplugClientDevice(d);
                        _devices.AddOrUpdate(d.DeviceIndex, dev, (idx, old) => dev);
                        _owningDispatcher.Send(_ =>
                        {
                            DeviceAdded?.Invoke(this, new DeviceEventArgs(dev, DeviceAction.ADDED));
                        }, null);
                        break;

                    case DeviceRemoved d:
                        if (_devices.TryRemove(d.DeviceIndex, out ButtplugClientDevice oldDev))
                        {
                            _owningDispatcher.Send(_ =>
                            {
                                DeviceRemoved?.Invoke(this, new DeviceEventArgs(oldDev, DeviceAction.REMOVED));
                            }, null);
                        }

                        break;

                    case ScanningFinished sf:
                        _owningDispatcher.Send(_ =>
                        {
                            ScanningFinished?.Invoke(this, new ScanningFinishedEventArgs(sf));
                        }, null);
                        break;

                    case Error e:
                        _owningDispatcher.Send(_ =>
                        {
                            ErrorReceived?.Invoke(this, new ErrorEventArgs(e));
                        }, null);
                        break;
                }
            }
        }

        private async void OnPingTimer(object state)
        {
            try
            {
                var msg = await SendMessage(new Ping());
                if (msg is Error)
                {
                    _owningDispatcher.Send(_ =>
                    {
                        ErrorReceived?.Invoke(this, new ErrorEventArgs(msg as Error));
                    }, null);
                    throw new Exception((msg as Error).ErrorMessage);
                }
            }
            catch
            {
                if (_ws != null)
                {
                    await Disconnect();
                }
            }
        }

        /// <summary>
        /// Instructs the server to start scanning for devices.
        /// New devices will be rasied as events to <see cref="DeviceAdded"/>.
        /// When scanning complets, an event will be sent to <see cref="ScanningFinished"/>.
        /// </summary>
        /// <returns>True if successful.</returns>
        public async Task<bool> StartScanning()
        {
            return await SendMessageExpectOk(new StartScanning());
        }

        /// <summary>
        /// Instructs the server to stop scanning for devices.
        /// If scanning was in progress, an event will be sent to <see cref="ScanningFinished"/> when the device managers have all stopped scanning.
        /// </summary>
        /// <returns>True if the server successful recieved the command. If there are errors when stoppong the device managers, events may be sent to <see cref="ErrorReceived"/></returns>
        public async Task<bool> StopScanning()
        {
            return await SendMessageExpectOk(new StopScanning());
        }

        /// <summary>
        /// Instructs the server to start forwarding log entries to the cleintf.
        /// Log entries will be rasied as events to <see cref="Log"/>.
        /// </summary>
        /// <param name="aLogLevel">The level of most detailed logs to send.</param>
        /// <returns>True if successful.</returns>
        public async Task<bool> RequestLog(string aLogLevel)
        {
            return await SendMessageExpectOk(new RequestLog(aLogLevel));
        }

        /// <summary>
        /// Sends a DeviceMessage (e.g. <see cref="VibrateCmd"/> or <see cref="LinearCmd"/>)
        /// </summary>
        /// <param name="aDevice">The device to be controlled by the message</param>
        /// <param name="aDeviceMsg">The device message (Id and DeviceIndex will be overriden)</param>
        /// <returns>True if successful.</returns>
        public async Task<ButtplugMessage> SendDeviceMessage(ButtplugClientDevice aDevice, ButtplugDeviceMessage aDeviceMsg)
        {
            if (_devices.TryGetValue(aDevice.Index, out ButtplugClientDevice dev))
            {
                if (!dev.AllowedMessages.ContainsKey(aDeviceMsg.GetType().Name))
                {
                    return new Error("Device does not accept message type: " + aDeviceMsg.GetType().Name, Error.ErrorClass.ERROR_DEVICE, ButtplugConsts.SystemMsgId);
                }

                aDeviceMsg.DeviceIndex = aDevice.Index;
                return await SendMessage(aDeviceMsg);
            }
            else
            {
                return new Error("Device not available.", Error.ErrorClass.ERROR_DEVICE, ButtplugConsts.SystemMsgId);
            }
        }

        /// <summary>
        /// Sends a message that we expect to respond with <see cref="Ok"/>
        /// </summary>
        /// <param name="aMsg">Message to send.</param>
        /// <returns>True if successful.</returns>
        protected async Task<bool> SendMessageExpectOk(ButtplugMessage aMsg)
        {
            return await SendMessage(aMsg) is Ok;
        }

        /// <summary>
        /// Sends a message and returns the resulting message
        /// </summary>
        /// <param name="aMsg">Message to send.</param>
        /// <returns>The response <see cref="ButtplugMessage"/></returns>
        protected async Task<ButtplugMessage> SendMessage(ButtplugMessage aMsg)
        {
            // The client always increments the IDs on outgoing messages
            aMsg.Id = NextMsgId;

            var promise = new TaskCompletionSource<ButtplugMessage>();
            _waitingMsgs.TryAdd(aMsg.Id, promise);

            var output = Serialize(aMsg);

            try
            {
                lock (_sendLock)
                {
                    if (_ws != null && _ws.State == WebSocketState.Open)
                    {
                        _ws.Send(output);
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

        /// <summary>
        /// Converts a single <see cref="ButtplugMessage"/> into a JSON string.
        /// </summary>
        /// <param name="aMsg">Message to convert</param>
        /// <returns>The JSON string representation of the message</returns>
        protected string Serialize(ButtplugMessage aMsg)
        {
            return _parser.Serialize(aMsg, ButtplugMessage.CurrentSchemaVersion);
        }

        /// <summary>
        /// Converts an array of <see cref="ButtplugMessage"/> into a JSON string.
        /// </summary>
        /// <param name="aMsgs">An array of messages to convert</param>
        /// <returns>The JSON string representation of the messages</returns>
        protected string Serialize(ButtplugMessage[] aMsgs)
        {
            return _parser.Serialize(aMsgs, ButtplugMessage.CurrentSchemaVersion);
        }

        /// <summary>
        /// Converts a JSON string into an array of <see cref="ButtplugMessage"/>.
        /// </summary>
        /// <param name="aMsg">A JSON string representing one or more messages</param>
        /// <returns>An array of messages</returns>
        protected ButtplugMessage[] Deserialize(string aMsg)
        {
            return _parser.Deserialize(aMsg);
        }
    }
}
