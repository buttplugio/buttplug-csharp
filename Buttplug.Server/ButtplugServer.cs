// <copyright file="ButtplugServer.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace Buttplug.Server
{
    public class ButtplugServer
    {
        /// <summary>
        /// Parser for Serializing/Deserializing JSON to ButtplugMessage objects.
        /// </summary>
        [NotNull]
        protected readonly ButtplugJsonMessageParser _parser;

        /// <summary>
        /// Token used for internal cancellation of async functions. Combined with external
        /// cancellation token to make sure we shut down when we expect.
        /// </summary>
        [NotNull]
        private readonly CancellationTokenSource _internalToken = new CancellationTokenSource();

        /// <summary>
        /// Event handler for messages exiting the server.
        /// </summary>
        [CanBeNull]
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        /// <summary>
        /// Event handler that fires whenever a client connects to a server.
        /// </summary>
        [CanBeNull]
        public event EventHandler ClientConnected;

        /// <summary>
        /// Event handler that fires on client ping timeouts.
        /// </summary>
        [CanBeNull]
        public event EventHandler PingTimeout;

        public string ClientName => _clientName ?? throw new InvalidOperationException("No client currently connected");

        /// <summary>
        /// Log manager that is passed to subclasses to make sure everything shares the same log object.
        /// </summary>
        [NotNull]
        protected readonly IButtplugLogManager BpLogManager;

        [CanBeNull] private string _clientName;

        /// <summary>
        /// Actual logger object, creates and stores log messages.
        /// </summary>
        [NotNull]
        private readonly IButtplugLog _bpLogger;

        /// <summary>
        /// Manages device subtype managers (usb, bluetooth, gamepad, etc), and relays device info to the Server.
        /// </summary>
        [NotNull]
        protected readonly DeviceManager _deviceManager;

        /// <summary>
        /// Timer that tracks ping updates from clients, if requested.
        /// </summary>
        [CanBeNull]
        private readonly Timer _pingTimer;

        /// <summary>
        /// The name of the server, as relayed to clients in <see cref="ServerInfo"/> messages.
        /// </summary>
        private readonly string _serverName;

        /// <summary>
        /// Maximum ping time for clients to send <see cref="Ping"/> messages in.
        /// </summary>
        private readonly uint _maxPingTime;

        private bool _pingTimedOut;

        /// <summary>
        /// True if client/server have successfully completed the <see cref="RequestServerInfo"/>/
        /// <see cref="ServerInfo"/> handshake.
        /// </summary>
        private bool _receivedRequestServerInfo;

        /// <summary>
        /// Spec version expected by the client. Used for stepping back on message versions.
        /// </summary>
        private uint _clientSpecVersion;

        public ButtplugServer(string aServerName, uint aMaxPing, DeviceManager aManager = null)
            : this(new ButtplugServerOptions(aServerName, aMaxPing, aManager))
        {
        }

        public ButtplugServer(ButtplugServerOptions aOptions)
        {
            ButtplugUtils.ArgumentNotNull(aOptions, nameof(aOptions));
            _clientName = null;
            _serverName = aOptions.ServerName;
            _maxPingTime = aOptions.MaxPingTime;
            _pingTimedOut = false;
            if (_maxPingTime != 0)
            {
                // Create a new timer that wont fire any events just yet
                _pingTimer = new Timer(PingTimeoutHandler, null, Timeout.Infinite, Timeout.Infinite);
            }

            BpLogManager = new ButtplugLogManager();
            _bpLogger = BpLogManager.GetLogger(GetType());
            _bpLogger.Debug("Setting up ButtplugServer");
            _parser = new ButtplugJsonMessageParser(BpLogManager);
            _deviceManager = aOptions.DeviceManager ?? new DeviceManager(BpLogManager, aOptions.SubtypeManagerSearchPaths);

            _bpLogger.Info("Finished setting up ButtplugServer");
            _deviceManager.DeviceMessageReceived += DeviceMessageReceivedHandler;
            _deviceManager.ScanningFinished += ScanningFinishedHandler;
            BpLogManager.LogMessageReceived += LogMessageReceivedHandler;
        }

        private void DeviceMessageReceivedHandler([NotNull] object aObj, [NotNull] MessageReceivedEventArgs aMsg)
        {
            MessageReceived?.Invoke(aObj, aMsg);
        }

        private void LogMessageReceivedHandler([NotNull] object aObj, [NotNull] ButtplugLogMessageEventArgs aEvent)
        {
            MessageReceived?.Invoke(aObj, new MessageReceivedEventArgs(aEvent.LogMessage));
        }

        private void ScanningFinishedHandler([NotNull] object aObj, EventArgs aEvent)
        {
            MessageReceived?.Invoke(aObj, new MessageReceivedEventArgs(new ScanningFinished()));
        }

        private async void PingTimeoutHandler([NotNull] object aObj)
        {
            // Stop the timer by specifying an infinite due period (See: https://msdn.microsoft.com/en-us/library/yz1c7148(v=vs.110).aspx)
            _pingTimer?.Change(Timeout.Infinite, (int)_maxPingTime);

            // Since this happens in an event handler, output a message, not an exception
            // TODO Once server has ErrorReceived event handlers, change this to throwing an exception through that.
            MessageReceived?.Invoke(this, new MessageReceivedEventArgs(new Error("Ping timed out.",
                Error.ErrorClass.ERROR_PING, ButtplugConsts.SystemMsgId)));
            await SendMessageAsync(new StopAllDevices()).ConfigureAwait(false);
            _pingTimedOut = true;
            PingTimeout?.Invoke(this, new EventArgs());
            _clientName = null;
        }

        [NotNull]
        public async Task<ButtplugMessage> SendMessageAsync([NotNull] ButtplugMessage aMsg, CancellationToken aToken = default(CancellationToken))
        {
            var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(_internalToken.Token, aToken);
            _bpLogger.Trace($"Got Message {aMsg.Id} of type {aMsg.GetType().Name} to send");
            var id = aMsg.Id;

            ButtplugUtils.ArgumentNotNull(aMsg, nameof(aMsg));

            if (id == ButtplugConsts.SystemMsgId)
            {
                throw new ButtplugMessageException(_bpLogger, "Message Id 0 is reserved for outgoing system messages. Please use another Id.", id);
            }

            if (aMsg is IButtplugMessageOutgoingOnly)
            {
                throw new ButtplugMessageException(_bpLogger, $"Message of type {aMsg.GetType().Name} cannot be sent to server", id);
            }

            if (_pingTimedOut)
            {
                throw new ButtplugPingException(_bpLogger, "Ping timed out.", id);
            }

            // If we get a message that's not RequestServerInfo first, return an error.
            if (!_receivedRequestServerInfo && !(aMsg is RequestServerInfo))
            {
                throw new ButtplugHandshakeException(_bpLogger, "RequestServerInfo must be first message received by server!", id);
            }

            _bpLogger.Debug($"Got {aMsg.Name} message.");

            switch (aMsg)
            {
                case RequestLog m:
                    BpLogManager.Level = m.LogLevel;
                    return new Ok(id);

                case Ping _:
                    // Start the timer
                    _pingTimer?.Change((int)_maxPingTime, (int)_maxPingTime);

                    return new Ok(id);

                case RequestServerInfo rsi:
                    if (_receivedRequestServerInfo)
                    {
                        throw new ButtplugHandshakeException(_bpLogger, "Already received RequestServerInfo, cannot be sent twice.", id);
                    }

                    _receivedRequestServerInfo = true;
                    _clientSpecVersion = rsi.MessageVersion;
                    _deviceManager.SpecVersion = _clientSpecVersion;

                    // Start the timer
                    _pingTimer?.Change((int)_maxPingTime, (int)_maxPingTime);
                    _clientName = rsi.ClientName;
                    ClientConnected?.Invoke(this, EventArgs.Empty);
                    return new ServerInfo(_serverName, ButtplugConsts.CurrentSpecVersion, _maxPingTime, id);

                case Test m:
                    return new Test(m.TestString, id);
            }

            return await _deviceManager.SendMessageAsync(aMsg, combinedToken.Token).ConfigureAwait(false);
        }

        public async Task ShutdownAsync(CancellationToken aToken = default(CancellationToken))
        {
            // Don't disconnect devices on shutdown, as they won't actually close.
            // Uncomment this once we figure out how to close bluetooth devices.
            // _deviceManager.RemoveAllDevices();
            var msg = await _deviceManager.SendMessageAsync(new StopAllDevices(), aToken).ConfigureAwait(false);
            if (msg is Error error)
            {
                _bpLogger.Error("An error occured while stopping devices on shutdown.");
                _bpLogger.Error(error.ErrorMessage);
            }

            _deviceManager.StopScanning();
            _deviceManager.DeviceMessageReceived -= DeviceMessageReceivedHandler;
            _deviceManager.ScanningFinished -= ScanningFinishedHandler;
            BpLogManager.LogMessageReceived -= LogMessageReceivedHandler;
        }

        [ItemNotNull]
        public async Task<ButtplugMessage[]> SendMessageAsync(string aJsonMsgs, CancellationToken aToken = default(CancellationToken))
        {
            var msgs = _parser.Deserialize(aJsonMsgs);
            var res = new List<ButtplugMessage>();
            foreach (var msg in msgs)
            {
                switch (msg)
                {
                    case Error errorMsg:
                        res.Add(errorMsg);
                        break;
                    default:
                        res.Add(await SendMessageAsync(msg, aToken).ConfigureAwait(false));
                        break;
                }
            }

            return res.ToArray();
        }

        public string Serialize(ButtplugMessage aMsg)
        {
            ButtplugUtils.ArgumentNotNull(aMsg, nameof(aMsg));

            return _parser.Serialize(aMsg, _clientSpecVersion);
        }

        public string Serialize(ButtplugMessage[] aMsgs)
        {
            ButtplugUtils.ArgumentNotNull(aMsgs, nameof(aMsgs));

            return _parser.Serialize(aMsgs, _clientSpecVersion);
        }

        // ReSharper disable once UnusedMember.Global
        public void AddDeviceSubtypeManager<T>(Func<IButtplugLogManager, T> aCreateMgrFunc)
            where T : IDeviceSubtypeManager
        {
            ButtplugUtils.ArgumentNotNull(aCreateMgrFunc, nameof(aCreateMgrFunc));

            _deviceManager.AddDeviceSubtypeManager(aCreateMgrFunc);
        }
    }
}
