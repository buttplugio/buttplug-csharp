using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Messages;
using JetBrains.Annotations;

namespace Buttplug.Server
{
    public class ButtplugServer
    {
        [NotNull]
        private readonly ButtplugJsonMessageParser _parser;

        [CanBeNull]
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        [CanBeNull]
        public event EventHandler<MessageReceivedEventArgs> ClientConnected;

        [NotNull]
        private readonly IButtplugLog _bpLogger;
        [NotNull]
        private DeviceManager _deviceManager;

        public DeviceManager DeviceManager
        {
            get => _deviceManager;
        }

        [NotNull]
        private readonly IButtplugLogManager _bpLogManager;
        private readonly Timer _pingTimer;

        private readonly string _serverName;
        private readonly uint _maxPingTime;
        private bool _pingTimedOut;
        private bool _receivedRequestServerInfo;
        private string _clientName;
        private uint _clientMessageVersion;

        public static string GetLicense()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Buttplug.Server.LICENSE";
            Stream stream = null;
            try
            {
                stream = assembly.GetManifestResourceStream(resourceName);
                using (var reader = new StreamReader(stream))
                {
                    stream = null;
                    return reader.ReadToEnd();
                }
            }
            finally
            {
                stream?.Dispose();
            }
        }

        public ButtplugServer([NotNull] string aServerName, uint aMaxPingTime, DeviceManager aDeviceManager = null)
        {
            _serverName = aServerName;
            _maxPingTime = aMaxPingTime;
            _pingTimedOut = false;
            if (aMaxPingTime != 0)
            {
                // Create a new timer that wont fire any events just yet
                _pingTimer = new Timer(PingTimeoutHandler, null, Timeout.Infinite, Timeout.Infinite);
            }

            _bpLogManager = new ButtplugLogManager();
            _bpLogger = _bpLogManager.GetLogger(GetType());
            _bpLogger.Debug("Setting up ButtplugServer");
            _parser = new ButtplugJsonMessageParser(_bpLogManager);
            if (aDeviceManager != null)
            {
                _deviceManager = aDeviceManager;
            }
            else
            {
                _deviceManager = new DeviceManager(_bpLogManager);
            }

            _bpLogger.Info("Finished setting up ButtplugServer");
            _deviceManager.DeviceMessageReceived += DeviceMessageReceivedHandler;
            _deviceManager.ScanningFinished += ScanningFinishedHandler;
            _bpLogManager.LogMessageReceived += LogMessageReceivedHandler;
        }

        private void DeviceMessageReceivedHandler([NotNull] object aObj, [NotNull] MessageReceivedEventArgs aMsg)
        {
            MessageReceived?.Invoke(aObj, aMsg);
        }

        private void LogMessageReceivedHandler([NotNull] object aObj, [NotNull] ButtplugLogMessageEventArgs aEvent)
        {
            MessageReceived?.Invoke(this, new MessageReceivedEventArgs(aEvent.LogMessage));
        }

        private void ScanningFinishedHandler([NotNull] object aObj, EventArgs aEvent)
        {
            MessageReceived?.Invoke(this, new MessageReceivedEventArgs(new ScanningFinished()));
        }

        private void PingTimeoutHandler([NotNull] object aObj)
        {
            // Stop the timer by specifying an infinate due period (See: https://msdn.microsoft.com/en-us/library/yz1c7148(v=vs.110).aspx)
            _pingTimer?.Change(Timeout.Infinite, (int)_maxPingTime);
            MessageReceived?.Invoke(this, new MessageReceivedEventArgs(new Error("Ping timed out.",
                Error.ErrorClass.ERROR_PING, ButtplugConsts.SystemMsgId)));
            SendMessage(new StopAllDevices()).Wait();
            _pingTimedOut = true;
        }

        [NotNull]
        public async Task<ButtplugMessage> SendMessage([NotNull] ButtplugMessage aMsg)
        {
            _bpLogger.Trace($"Got Message {aMsg.Id} of type {aMsg.GetType().Name} to send");
            var id = aMsg.Id;
            if (id == 0)
            {
                return _bpLogger.LogWarnMsg(id, Error.ErrorClass.ERROR_MSG,
                    "Message Id 0 is reserved for outgoing system messages. Please use another Id.");
            }

            if (aMsg is IButtplugMessageOutgoingOnly)
            {
                return _bpLogger.LogWarnMsg(id, Error.ErrorClass.ERROR_MSG,
                    $"Message of type {aMsg.GetType().Name} cannot be sent to server");
            }

            if (_pingTimedOut)
            {
                return _bpLogger.LogErrorMsg(id, Error.ErrorClass.ERROR_PING, "Ping timed out.");
            }

            // If we get a message that's not RequestServerInfo first, return an error.
            if (!_receivedRequestServerInfo && !(aMsg is RequestServerInfo))
            {
                return _bpLogger.LogErrorMsg(id, Error.ErrorClass.ERROR_INIT,
                    "RequestServerInfo must be first message received by server!");
            }

            switch (aMsg)
            {
                case RequestLog m:
                    _bpLogger.Debug("Got RequestLog Message");
                    _bpLogManager.Level = m.LogLevel;
                    return new Ok(id);

                case Ping _:
                    if (_pingTimer != null)
                    {
                        // Start the timer
                        _pingTimer.Change((int)_maxPingTime, (int)_maxPingTime);
                    }

                    return new Ok(id);

                case RequestServerInfo rsi:
                    _bpLogger.Debug("Got RequestServerInfo Message");
                    _receivedRequestServerInfo = true;
                    _clientMessageVersion = rsi?.MessageVersion ?? 0;

                    // Start the timer
                    _pingTimer?.Change((int)_maxPingTime, (int)_maxPingTime);
                    ClientConnected?.Invoke(this, new MessageReceivedEventArgs(rsi));
                    return new ServerInfo(_serverName, 1, _maxPingTime, id);

                case Test m:
                    return new Test(m.TestString, id);
            }

            return await _deviceManager.SendMessage(aMsg);
        }

        public async Task Shutdown()
        {
            // Don't disconnect devices on shutdown, as they won't actually close.
            // Uncomment this once we figure out how to close bluetooth devices.
            // _deviceManager.RemoveAllDevices();
            var msg = await _deviceManager.SendMessage(new StopAllDevices());
            if (msg is Error)
            {
                _bpLogger.Error("An error occured while stopping devices on shutdown.");
                _bpLogger.Error((msg as Error).ErrorMessage);
            }

            _deviceManager.StopScanning();
            _deviceManager.DeviceMessageReceived -= DeviceMessageReceivedHandler;
            _deviceManager.ScanningFinished -= ScanningFinishedHandler;
            _bpLogManager.LogMessageReceived -= LogMessageReceivedHandler;
        }

        [ItemNotNull]
        public async Task<ButtplugMessage[]> SendMessage(string aJsonMsgs)
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
                        res.Add(await SendMessage(msg));
                        break;
                }
            }

            return res.ToArray();
        }

        public string Serialize(ButtplugMessage aMsg)
        {
            return _parser.Serialize(aMsg, _clientMessageVersion);
        }

        public string Serialize(ButtplugMessage[] aMsgs)
        {
            return _parser.Serialize(aMsgs, _clientMessageVersion);
        }

        public ButtplugMessage[] Deserialize(string aMsg)
        {
            return _parser.Deserialize(aMsg);
        }

        public void AddDeviceSubtypeManager<T>(Func<IButtplugLogManager, T> aCreateMgrFunc)
            where T : IDeviceSubtypeManager
        {
            _deviceManager.AddDeviceSubtypeManager(aCreateMgrFunc);
        }

        internal void AddDeviceSubtypeManager(IDeviceSubtypeManager aMgr)
        {
            _deviceManager.AddDeviceSubtypeManager(aMgr);
        }

        // ReSharper disable once UnusedMember.Global
        [NotNull]
        internal DeviceManager GetDeviceManager()
        {
            return _deviceManager;
        }
    }
}
