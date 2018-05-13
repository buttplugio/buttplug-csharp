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

namespace Buttplug.Client
{
    public abstract class ButtplugAbsractClient : IButtplugClient
    {
        /// <summary>
        /// Used for converting messages between JSON and Objects.
        /// </summary>
        [NotNull]
        protected readonly ButtplugJsonMessageParser _parser;

        /// <summary>
        /// Global logger instance for the client.
        /// </summary>
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        [NotNull]
        protected readonly IButtplugLog _bpLogger;

        /// <summary>
        /// Name of the client, used for server UI/permissions.
        /// </summary>
        [NotNull]
        protected readonly string _clientName;

        /// <summary>
        /// Used for canceling out of websocket wait loops.
        /// </summary>
        [NotNull]
        protected CancellationTokenSource _tokenSource;

        /// <summary>
        /// Used for dispatching events to the owning application context.
        /// </summary>
        [NotNull]
        protected readonly SynchronizationContext _owningDispatcher;

        /// <summary>
        /// Stores messages waiting for reply from the server.
        /// </summary>
        [NotNull]
        protected readonly ConcurrentDictionary<uint, TaskCompletionSource<ButtplugMessage>> _waitingMsgs =
            new ConcurrentDictionary<uint, TaskCompletionSource<ButtplugMessage>>();

        /// <summary>
        /// Stores information about devices currently connected to the server.
        /// </summary>
        [NotNull]
        protected readonly ConcurrentDictionary<uint, ButtplugClientDevice> _devices =
            new ConcurrentDictionary<uint, ButtplugClientDevice>();

        /// <summary>
        /// Ping timer.
        /// </summary>
        /// <remarks>
        /// Sends a ping message to the server whenever the timer triggers. Usually runs at
        /// (requested ping interval / 2).
        /// </remarks>
        [CanBeNull] protected Timer _pingTimer;

        /// <summary>
        /// Event fired on Buttplug device added, either after connect or while scanning for devices.
        /// </summary>
        [CanBeNull]
        public virtual event EventHandler<DeviceEventArgs> DeviceAdded;

        /// <summary>
        /// Event fired on Buttplug device removed. Can fire at any time after device connection.
        /// </summary>
        [CanBeNull]
        public virtual event EventHandler<DeviceEventArgs> DeviceRemoved;

        /// <summary>
        /// Event fired when the server has finished scanning for devices.
        /// </summary>
        [CanBeNull]
        public virtual event EventHandler<ScanningFinishedEventArgs> ScanningFinished;

        /// <summary>
        /// Event fired when an error has been encountered. This may be internal client exceptions or
        /// Error messages from the server.
        /// </summary>
        [CanBeNull]
        public virtual event EventHandler<ErrorEventArgs> ErrorReceived;

        /// <summary>
        /// Event fired when the client receives a Log message. Should only fire if the client has
        /// requested that log messages be sent.
        /// </summary>
        [CanBeNull]
        public virtual event EventHandler<LogEventArgs> Log;

        /// <summary>
        /// Gets the next available message ID. In most cases, setting the message ID is done automatically.
        /// </summary>
        public uint NextMsgId => Convert.ToUInt32(Interlocked.Increment(ref _counter));

        /// <summary>
        /// Stores the last used current message ID.
        /// </summary>
        protected int _counter;

        /// <summary>
        /// Gets the connected Buttplug devices.
        /// </summary>
        public ButtplugClientDevice[] Devices => _devices.Values.ToArray();

        /// <summary>
        /// Status of the client connection.
        /// </summary>
        /// <returns>True if client is currently connected.</returns>
        public abstract bool IsConnected { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="IButtplugClient"/> class.
        /// </summary>
        /// <param name="aClientName">The name of the client (used by the server for UI and permissions).</param>
        protected ButtplugAbsractClient(string aClientName)
        {
            _clientName = aClientName;
            IButtplugLogManager bpLogManager = new ButtplugLogManager();
            _bpLogger = bpLogManager.GetLogger(GetType());
            _parser = new ButtplugJsonMessageParser(bpLogManager);
            _bpLogger.Info("Finished setting up ButtplugClient");
            _owningDispatcher = SynchronizationContext.Current ?? new SynchronizationContext();
            _tokenSource = new CancellationTokenSource();
            _counter = 0;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="ButtplugAbstractClient"/> class, closing the transport
        /// connection if it is still open.
        /// </summary>
        ~ButtplugAbsractClient()
        {
            Disconnect().Wait();
        }

        /// <summary>
        /// Creates the connection to the Buttplug Server and performs the protocol handshake.
        /// </summary>
        /// <remarks>
        /// Once the transport connection is open, the RequestServerInfo message is sent; the
        /// response is used to set up the ping timer loop. The RequestDeviceList message is also
        /// sent, so that any devices the server is already connected to are made known to the client.
        ///
        /// <b>Important:</b> Ensure that <see cref="DeviceAdded"/>, <see cref="DeviceRemoved"/> and
        /// <see cref="ErrorReceived"/> handlers are set before Connect is called.
        /// </remarks>
        /// <param name="aURL">
        /// The URI for the Buttplug Server over the specified transport.
        /// </param>
        /// <param name="aIgnoreSSLErrors">
        /// When using SSL (wss://), prevents bad certificates from causing connection failures
        /// </param>
        /// <returns>Nothing (Task used for async/await)</returns>
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Embedded acronyms")]
        public abstract Task Connect(Uri aURL, bool aIgnoreSSLErrors = false);

        /// <summary>
        /// Closes the transport connection.
        /// </summary>
        /// <returns>Nothing (Task used for async/await)</returns>
        public abstract Task Disconnect();

        /// <summary>
        /// Buttplug Message Received handler. Either tries to match incoming messages as
        /// replies to messages we've sent, or fires an event related to an incoming event, like
        /// device additions/removals, log messages, etc.
        /// </summary>
        /// <param name="aMsgs">An array of deserialized Buttplug messages.</param>
        protected void MessageReceivedHandler(ButtplugMessage[] aMsgs)
        {
            foreach (var msg in aMsgs)
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
                            DeviceAdded?.Invoke(this, new DeviceEventArgs(dev, DeviceEventArgs.DeviceAction.ADDED));
                        }, null);
                        break;

                    case DeviceRemoved d:
                        if (_devices.TryRemove(d.DeviceIndex, out ButtplugClientDevice oldDev))
                        {
                            _owningDispatcher.Send(_ =>
                            {
                                DeviceRemoved?.Invoke(this, new DeviceEventArgs(oldDev, DeviceEventArgs.DeviceAction.REMOVED));
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

        /// <summary>
        /// Manages the ping timer, sending pings at the rate faster than requested by the server. If
        /// the ping timer handler does not run, it means the event loop is blocked, and the server
        /// will stop all devices and disconnect.
        /// </summary>
        /// <param name="aState">State of the Timer.</param>
        protected async void OnPingTimer(object state)
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
                await Disconnect();
            }
        }

        /// <summary>
        /// Instructs the server to start scanning for devices. New devices will be raised as <see
        /// cref="DeviceAdded"/> events. When scanning completes, an <see cref="ScanningFinished"/>
        /// event will be triggered.
        /// </summary>
        /// <returns>
        /// True if successful. If there are errors while the server is starting scanning, a <see
        /// cref="ErrorReceived"/> event will be triggered.
        /// </returns>
        public async Task<bool> StartScanning()
        {
            return await SendMessageExpectOk(new StartScanning());
        }

        /// <summary>
        /// Instructs the server to stop scanning for devices. If scanning was in progress, a <see
        /// cref="ScanningFinished"/> event will be sent when the server has stopped scanning.
        /// </summary>
        /// <returns>
        /// True if the server successful recieved the command. If there are errors while the server
        /// is stopping scanning, a <see cref="ErrorReceived"/> event will be triggered.
        /// </returns>
        public async Task<bool> StopScanning()
        {
            return await SendMessageExpectOk(new StopScanning());
        }

        /// <summary>
        /// Instructs the server to either forward or stop log entries to the client. Log entries
        /// will be raised as <see cref="Log"/> events.
        /// </summary>
        /// <param name="aLogLevel">The maximum log level to send.</param>
        /// <returns>
        /// True if successful. If there are errors while the server is stopping scanning, a <see
        /// cref="ErrorReceived"/> event will be triggered.
        /// </returns>
        public async Task<bool> RequestLog(string aLogLevel)
        {
            return await SendMessageExpectOk(new RequestLog(aLogLevel));
        }

        /// <summary>
        /// Sends a DeviceMessage (e.g. <see cref="VibrateCmd"/> or <see cref="LinearCmd"/>). Handles
        /// constructing some parts of the message for the user.
        /// </summary>
        /// <param name="aDevice">The device to be controlled by the message.</param>
        /// <param name="aDeviceMsg">The device message (Id and DeviceIndex will be overriden).</param>
        /// <returns>
        /// <see cref="Ok"/> message on success, <see cref="Error"/> message with error info otherwise.
        /// </returns>
        public async Task<ButtplugMessage> SendDeviceMessage(ButtplugClientDevice aDevice, ButtplugDeviceMessage aDeviceMsg)
        {
            if (!_devices.TryGetValue(aDevice.Index, out ButtplugClientDevice dev))
            {
                return new Error("Device not available.", Error.ErrorClass.ERROR_DEVICE, ButtplugConsts.SystemMsgId);
            }

            if (!dev.AllowedMessages.ContainsKey(aDeviceMsg.GetType().Name))
            {
                return new Error("Device does not accept message type: " + aDeviceMsg.GetType().Name, Error.ErrorClass.ERROR_DEVICE, ButtplugConsts.SystemMsgId);
            }

            aDeviceMsg.DeviceIndex = aDevice.Index;
            return await SendMessage(aDeviceMsg);
        }

        /// <summary>
        /// Sends a message, expecting a response of message type <see cref="Ok"/>.
        /// </summary>
        /// <param name="aMsg">Message to send.</param>
        /// <returns>True if successful.</returns>
        protected async Task<bool> SendMessageExpectOk(ButtplugMessage aMsg)
        {
            return await SendMessage(aMsg) is Ok;
        }

        /// <summary>
        /// Sends a message to the server, and handles asynchronously waiting for the reply from the server.
        /// </summary>
        /// <param name="aMsg">Message to send.</param>
        /// <returns>The response, which will derive from <see cref="ButtplugMessage"/>.</returns>
        protected abstract Task<ButtplugMessage> SendMessage(ButtplugMessage aMsg);

        /// <summary>
        /// Converts a single <see cref="ButtplugMessage"/> into a JSON string.
        /// </summary>
        /// <param name="aMsg">Message to convert.</param>
        /// <returns>The JSON string representation of the message.</returns>
        protected string Serialize(ButtplugMessage aMsg)
        {
            return _parser.Serialize(aMsg, ButtplugMessage.CurrentSchemaVersion);
        }

        /// <summary>
        /// Converts an array of <see cref="ButtplugMessage"/> into a JSON string.
        /// </summary>
        /// <param name="aMsgs">An array of messages to convert.</param>
        /// <returns>The JSON string representation of the messages.</returns>
        protected string Serialize(ButtplugMessage[] aMsgs)
        {
            return _parser.Serialize(aMsgs, ButtplugMessage.CurrentSchemaVersion);
        }

        /// <summary>
        /// Converts a JSON string into an array of <see cref="ButtplugMessage"/>.
        /// </summary>
        /// <param name="aMsg">A JSON string representing one or more messages.</param>
        /// <returns>An array of <see cref="ButtplugMessage"/>.</returns>
        protected ButtplugMessage[] Deserialize(string aMsg)
        {
            return _parser.Deserialize(aMsg);
        }
    }
}