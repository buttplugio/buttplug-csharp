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

        [CanBeNull]
        public event EventHandler<DeviceEventArgs> DeviceAdded;

        [CanBeNull]
        public event EventHandler<DeviceEventArgs> DeviceRemoved;

        [CanBeNull]
        public event EventHandler<ScanningFinishedEventArgs> ScanningFinished;

        [CanBeNull]
        public event EventHandler<ErrorEventArgs> ErrorReceived;

        [CanBeNull]
        public event EventHandler<LogEventArgs> Log;

        private TaskCompletionSource<bool> _connectedOrFailed;

        private TaskCompletionSource<bool> _disconnected;

        private bool _connecting;

        public uint NextMsgId => Convert.ToUInt32(Interlocked.Increment(ref _counter));

        public ButtplugClientDevice[] Devices => _devices.Values.ToArray();

        /// <summary>
        /// Is the client connected?
        /// </summary>
        public bool IsConnected => _ws != null &&
                                   (_ws.State == WebSocketState.Connecting ||
                                    _ws.State == WebSocketState.Open);

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

        ~ButtplugWSClient()
        {
            Disconnect().Wait();
        }

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

        public async Task<bool> StartScanning()
        {
            return await SendMessageExpectOk(new StartScanning());
        }

        public async Task<bool> StopScanning()
        {
            return await SendMessageExpectOk(new StopScanning());
        }

        public async Task<bool> RequestLog(string aLogLevel)
        {
            return await SendMessageExpectOk(new RequestLog(aLogLevel));
        }

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

        protected async Task<bool> SendMessageExpectOk(ButtplugMessage aMsg)
        {
            return await SendMessage(aMsg) is Ok;
        }

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

        protected string Serialize(ButtplugMessage aMsg)
        {
            return _parser.Serialize(aMsg, ButtplugMessage.CurrentSchemaVersion);
        }

        protected string Serialize(ButtplugMessage[] aMsgs)
        {
            return _parser.Serialize(aMsgs, ButtplugMessage.CurrentSchemaVersion);
        }

        protected ButtplugMessage[] Deserialize(string aMsg)
        {
            return _parser.Deserialize(aMsg);
        }
    }
}
