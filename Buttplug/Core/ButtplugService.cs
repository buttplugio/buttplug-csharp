using Buttplug.Messages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Buttplug.Logging;

namespace Buttplug.Core
{

    public class ButtplugService
    {
        private readonly ButtplugJsonMessageParser _parser;
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        internal readonly ButtplugLog _bpLogger;
        private readonly DeviceManager _deviceManager;
        private readonly ButtplugLogManager _bpLogManager;

        public ButtplugService()
        {
            _bpLogManager = new ButtplugLogManager();
            _bpLogger = _bpLogManager.GetLogger(LogProvider.GetCurrentClassLogger());
            _bpLogger.Trace("Setting up ButtplugService");
            _parser = new ButtplugJsonMessageParser(_bpLogManager);
            _deviceManager = new DeviceManager(_bpLogManager);
            _bpLogger.Trace("Finished setting up ButtplugService");
            _deviceManager.DeviceMessageReceived += DeviceMessageReceivedHandler;
            _bpLogManager.LogMessageReceived += LogMessageReceivedHandler;
        }

        public void DeviceMessageReceivedHandler(object o, MessageReceivedEventArgs aMsg)
        {
            MessageReceived?.Invoke(o, aMsg);
        }

        private void LogMessageReceivedHandler(object o, ButtplugLogMessageEventArgs e)
        {
            MessageReceived?.Invoke(this, new MessageReceivedEventArgs(e.LogMessage));
        }

        public async Task<ButtplugMessage> SendMessage(ButtplugMessage aMsg)
        {
            _bpLogger.Trace($"Got Message {aMsg.Id} of type {aMsg.GetType().Name} to send");
            var id = aMsg.Id;
            if (id == 0)
            {
                return ButtplugUtils.LogWarnMsg(id, _bpLogger,
                    $"Message Id 0 is reserved for outgoing system messages. Please use another Id.");
            }
            if (aMsg is IButtplugMessageOutgoingOnly)
            {
                return ButtplugUtils.LogWarnMsg(id, _bpLogger,
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

        public async Task<ButtplugMessage> SendMessage(string aJsonMsg)
        {
            var msg = _parser.Deserialize(aJsonMsg);
            return await msg.MatchAsync(
                async x => await SendMessage(x),
                x => ButtplugUtils.LogErrorMsg(ButtplugConsts.SYSTEM_MSG_ID, _bpLogger,
                        $"Cannot deserialize json message: {x}"));
        }

        public string Serialize(ButtplugMessage aMsg)
        {
            return ButtplugJsonMessageParser.Serialize(aMsg);
        }

        public void AddDeviceSubtypeManager<T>(Func<ButtplugLogManager,T> aCreateMgrFunc) where T : IDeviceSubtypeManager
        {
            _deviceManager.AddDeviceSubtypeManager(aCreateMgrFunc);
        }

        internal void AddDeviceSubtypeManager(IDeviceSubtypeManager mgr)
        {
            _deviceManager.AddDeviceSubtypeManager(mgr);
        }

        internal DeviceManager GetDeviceManager()
        {
            return _deviceManager;
        }
    }
}