using Buttplug.Messages;
using JetBrains.Annotations;
using System;
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

        public ButtplugService()
        {
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
            switch (aMsg)
            {
                case RequestLog m:
                    _bpLogManager.Level = m.LogLevel;
                    return new Ok(id);

                case RequestServerInfo _:
                    return new ServerInfo(id);

                case Test m:
                    return new Test(m.TestString, id);
            }
            return await _deviceManager.SendMessage(aMsg);
        }

        [ItemNotNull]
        public async Task<ButtplugMessage> SendMessage(string aJsonMsg)
        {
            var msg = _parser.Deserialize(aJsonMsg);
            switch (msg)
            {
                case Error errorMsg:
                    return errorMsg;
                default:
                    return await SendMessage(msg);
            }
        }

        public static string Serialize(ButtplugMessage aMsg)
        {
            return ButtplugJsonMessageParser.Serialize(aMsg);
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

