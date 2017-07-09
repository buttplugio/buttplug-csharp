using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using Buttplug.Core;
using Buttplug.Messages;
using JetBrains.Annotations;
using static Buttplug.Messages.Error;

namespace Buttplug.Server
{
    public class ButtplugService
    {
        [NotNull]
        private readonly ButtplugJsonMessageParser _parser;

        [CanBeNull]
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        [NotNull]
        private readonly IButtplugLog _bpLogger;
        [NotNull]
        private readonly DeviceManager _deviceManager;
        [NotNull]
        private readonly IButtplugLogManager _bpLogManager;
        private readonly Timer _pingTimer;

        private readonly string _serverName;
        private readonly uint _maxPingTime;
        private bool _pingTimedOut;
        private bool _receivedRequestServerInfo;

        public static string GetLicense()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Buttplug.LICENSE";
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

        public ButtplugService([NotNull] string aServerName, uint aMaxPingTime)
        {
            _serverName = aServerName;
            _maxPingTime = aMaxPingTime;
            _pingTimedOut = false;
            if (aMaxPingTime != 0)
            {
                _pingTimer = new Timer(_maxPingTime);
                _pingTimer.Elapsed += PingTimeoutHandler;
            }

            _bpLogManager = new ButtplugLogManager();
            _bpLogger = _bpLogManager.GetLogger(GetType());
            _bpLogger.Trace("Setting up ButtplugService");
            _parser = new ButtplugJsonMessageParser(_bpLogManager);
            _deviceManager = new DeviceManager(_bpLogManager);
            _bpLogger.Trace("Finished setting up ButtplugService");
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

        private void PingTimeoutHandler([NotNull] object aObj, EventArgs e)
        {
            _pingTimer?.Stop();
            MessageReceived?.Invoke(this, new MessageReceivedEventArgs(new Error("Ping timed out.",
                ErrorClass.ERROR_PING, ButtplugConsts.SystemMsgId)));
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
                return _bpLogger.LogWarnMsg(id, ErrorClass.ERROR_MSG,
                    "Message Id 0 is reserved for outgoing system messages. Please use another Id.");
            }

            if (aMsg is IButtplugMessageOutgoingOnly)
            {
                return _bpLogger.LogWarnMsg(id, ErrorClass.ERROR_MSG,
                    $"Message of type {aMsg.GetType().Name} cannot be sent to server");
            }

            if (_pingTimedOut)
            {
                return _bpLogger.LogErrorMsg(id, ErrorClass.ERROR_PING, "Ping timed out.");
            }

            // If we get a message that's not RequestServerInfo first, return an error.
            if (!_receivedRequestServerInfo && !(aMsg is RequestServerInfo))
            {
                return _bpLogger.LogErrorMsg(id, ErrorClass.ERROR_INIT,
                    "RequestServerInfo must be first message received by server!");
            }

            switch (aMsg)
            {
                case RequestLog m:
                    _bpLogManager.Level = m.LogLevel;
                    return new Ok(id);

                case Ping _:
                    if (_pingTimer != null)
                    {
                        _pingTimer.Stop();
                        _pingTimer.Start();
                    }

                    return new Ok(id);

                case RequestServerInfo _:
                    _receivedRequestServerInfo = true;
                    _pingTimer?.Start();
                    return new ServerInfo(_serverName, 1, _maxPingTime, id);

                case Test m:
                    return new Test(m.TestString, id);
            }

            return await _deviceManager.SendMessage(aMsg);
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
            return _parser.Serialize(aMsg);
        }

        public string Serialize(ButtplugMessage[] aMsgs)
        {
            return _parser.Serialize(aMsgs);
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