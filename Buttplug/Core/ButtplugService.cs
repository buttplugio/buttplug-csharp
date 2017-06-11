using Buttplug.Messages;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Buttplug.Core
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

        private readonly string _serverName;
        private uint _maxPingTime;
        private readonly uint _messageSchemaVersion;
        private bool _receivedRequestServerInfo;

        public ButtplugService(string aServerName, uint aMaxPingTime)
        {
            _serverName = aServerName;
            _maxPingTime = aMaxPingTime;
            _bpLogManager = new ButtplugLogManager();
            _bpLogger = _bpLogManager.GetLogger(GetType());
            _bpLogger.Trace("Setting up ButtplugService");
            _parser = new ButtplugJsonMessageParser(_bpLogManager);
            _deviceManager = new DeviceManager(_bpLogManager);
            _bpLogger.Trace("Finished setting up ButtplugService");
            _deviceManager.DeviceMessageReceived += DeviceMessageReceivedHandler;
            _bpLogManager.LogMessageReceived += LogMessageReceivedHandler;
        }

        private void DeviceMessageReceivedHandler([NotNull] object aObj, [NotNull] MessageReceivedEventArgs aMsg)
        {
            MessageReceived?.Invoke(aObj, aMsg);
        }

        private void LogMessageReceivedHandler([NotNull] object aObj, [NotNull] ButtplugLogMessageEventArgs e)
        {
            MessageReceived?.Invoke(this, new MessageReceivedEventArgs(e.LogMessage));
        }

        [NotNull]
        public async Task<ButtplugMessage> SendMessage([NotNull] ButtplugMessage aMsg)
        {
            _bpLogger.Trace($"Got Message {aMsg.Id} of type {aMsg.GetType().Name} to send");
            var id = aMsg.Id;
            if (id == 0)
            {
                return _bpLogger.LogWarnMsg(id,
                    $"Message Id 0 is reserved for outgoing system messages. Please use another Id.");
            }
            if (aMsg is IButtplugMessageOutgoingOnly)
            {
                return _bpLogger.LogWarnMsg(id,
                    $"Message of type {aMsg.GetType().Name} cannot be sent to server");
            }

            // If we get a message that's not RequestServerInfo first, return an error.
            if (!_receivedRequestServerInfo && !(aMsg is RequestServerInfo))
            {
                return _bpLogger.LogErrorMsg(id,
                    $"RequestServerInfo must be first message received by server!");
            }
            switch (aMsg)
            {
                case RequestLog m:
                    _bpLogManager.Level = m.LogLevel;
                    return new Ok(id);

                case RequestServerInfo _:
                    _receivedRequestServerInfo = true;
                    return new ServerInfo(_serverName, 1, _maxPingTime, id);

                case Test m:
                    return new Test(m.TestString, id);
            }
            return await _deviceManager.SendMessage(aMsg);
        }

        [ItemNotNull]
        public async Task<ButtplugMessage[]> SendMessage(string aJsonMsgs)
        {
            ButtplugMessage[] msgs = _parser.Deserialize(aJsonMsgs);
            List<ButtplugMessage> res = new List<ButtplugMessage>();
            foreach (ButtplugMessage msg in msgs)
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

        public void AddDeviceSubtypeManager<T>(Func<IButtplugLogManager,T> aCreateMgrFunc) where T : IDeviceSubtypeManager
        {
            _deviceManager.AddDeviceSubtypeManager(aCreateMgrFunc);
        }

        internal void AddDeviceSubtypeManager(IDeviceSubtypeManager mgr)
        {
            _deviceManager.AddDeviceSubtypeManager(mgr);
        }

        // ReSharper disable once UnusedMember.Global
        [NotNull]
        internal DeviceManager GetDeviceManager()
        {
            return _deviceManager;
        }
    }
}

